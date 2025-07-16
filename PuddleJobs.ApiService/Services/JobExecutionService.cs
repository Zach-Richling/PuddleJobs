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
        var fireInstanceId = long.Parse(context.FireInstanceId);

        jobData["Logger"] = _jobLogger;

        var assemblyContext = new AssemblyLoadContext("UserJobContext", isCollectible: true);

        var executionLog = new ExecutionLog() 
        { 
            FireInstanceId = long.Parse(context.FireInstanceId),
            JobId = jobId,
            StartTime = DateTime.UtcNow,
            Status = "Running"
        };

        _context.ExecutionLogs.Add(executionLog);
        await _context.SaveChangesAsync();

        using (LogContext.PushProperty("FireInstanceId", fireInstanceId))
        {
            _logger.LogInformation("Starting job");

            try
            {
                var job = await _context.Jobs
                    .Include(j => j.Assembly)
                        .ThenInclude(a => a.Versions)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(j => j.Id == jobId)
                    ?? throw new InvalidOperationException($"Job not found.");

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
                    executionLog.Status = "Success";
                    _logger.LogInformation("Job run succeeded");
                } 
                catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
                {
                    executionLog.Status = "Cancelled";
                    _logger.LogInformation("Job run cancelled");
                }
                catch (Exception e)
                {
                    executionLog.Status = "Failed";
                    _logger.LogError(e, "Exception during job run");
                }
            }
            catch (Exception e)
            {
                executionLog.Status = "Failed";
                _logger.LogError(e, "Could not start job");
            }
            finally
            {
                executionLog.EndTime = DateTime.UtcNow;
                try
                {
                    await _context.SaveChangesAsync();
                } 
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not save job execution");
                } 
                finally
                {
                    assemblyContext.Unload();
                }
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