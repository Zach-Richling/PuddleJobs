using Microsoft.EntityFrameworkCore;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.Helpers;
using PuddleJobs.ApiService.Jobs;
using PuddleJobs.ApiService.Models;
using Quartz;
using Serilog.Context;
using System.Runtime.Loader;

namespace PuddleJobs.ApiService.Services;

public interface IJobExecutionService
{
    Task ExecuteJobAsync(IJobExecutionContext context);
}

public class JobExecutionService : IJobExecutionService
{
    private readonly JobSchedulerDbContext _context;
    private readonly IAssemblyStorageService _assemblyStorageService;
    private readonly ILogger<JobExecutionService> _logger;
    private readonly ILogger<PuddleJob> _jobLogger;

    public JobExecutionService(JobSchedulerDbContext context, IAssemblyStorageService assemblyStorageService, ILogger<JobExecutionService> logger, ILogger<PuddleJob> jobLogger)
    {
        _context = context;
        _assemblyStorageService = assemblyStorageService;
        _logger = logger;
        _jobLogger = jobLogger;
    }

    public async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        var jobData = context.JobDetail.JobDataMap;
        var jobId = jobData.GetInt("jobId");

        jobData["Logger"] = _jobLogger;

        var assemblyContext = new AssemblyLoadContext("MyContext", isCollectible: true);

        using (LogContext.PushProperty("FireInstanceId", context.FireInstanceId))
        using (LogContext.PushProperty("JobId", jobId))
        {
            try
            {
                var job = await _context.Jobs
                    .Include(j => j.Assembly)
                        .ThenInclude(a => a.Versions)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(j => j.Id == jobId)
                    ?? throw new InvalidOperationException($"Job with ID {jobId} not found.");

                var activeAssembly = job.Assembly.ActiveVersion;
                var parameters = await LoadJobParametersAsync(jobId);
                foreach (var param in parameters.Where(x => x.Value != null))
                {
                    jobData[param.Key] = param.Value!;
                }

                var assembly = await _assemblyStorageService.LoadAssemblyVersionAsync(activeAssembly, assemblyContext);

                var jobType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IJob).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    ?? throw new InvalidOperationException($"No job type implementing Quartz.IJob.");

                var jobInstance = (IJob?)Activator.CreateInstance(jobType)
                    ?? throw new InvalidOperationException($"Failed to create instance of job type '{jobType.Name}'.");

                try
                {
                    await jobInstance.Execute(context);
                }
                catch (Exception ex)
                {
                    using (LogContext.PushProperty("JobOutcome", 0))
                    {
                        _logger.LogError(ex, "Exception during job run");
                    }
                    return;
                }

                using (LogContext.PushProperty("JobOutcome", 1))
                {
                    _logger.LogInformation("Job executed successfully");
                }
            }
            catch (Exception ex)
            {
                using (LogContext.PushProperty("JobOutcome", 0))
                {
                    _logger.LogError(ex, "Could not start job");
                }
            }
            finally
            {
                assemblyContext.Unload();
            }
        }
    }

    private async Task<Dictionary<string, object?>> LoadJobParametersAsync(int jobId)
    {
        var result = new Dictionary<string, object?>();

        var job = await _context.Jobs
            .Include(j => j.Parameters)
            .Include(j => j.Assembly)
                .ThenInclude(a => a.Versions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(j => j.Id == jobId) 
            ?? throw new InvalidOperationException($"Job with ID {jobId} not found.");

        var parameterDefinitions = _context.AssemblyParameterDefinitions
            .Where(pd => pd.AssemblyVersionId == job.Assembly.ActiveVersion.Id)
            .ToList();

        foreach (var paramDef in parameterDefinitions)
        {
            var dbParameter = job.Parameters.FirstOrDefault(p => p.Name == paramDef.Name);
            
            if (dbParameter != null && !string.IsNullOrEmpty(dbParameter.Value))
            {
                var convertedValue = JobParameterHelper.ConvertJobParameterValue(dbParameter.Value, paramDef.Type);
                result[paramDef.Name] = convertedValue;
            }
            else if (!string.IsNullOrEmpty(paramDef.DefaultValue))
            {
                var convertedValue = JobParameterHelper.ConvertJobParameterValue(paramDef.DefaultValue, paramDef.Type);
                result[paramDef.Name] = convertedValue;
            }
            else if (paramDef.Required)
            {
                throw new InvalidOperationException($"Required parameter '{paramDef.Name}' has no value and no default.");
            }
        }

        return result;
    }
} 