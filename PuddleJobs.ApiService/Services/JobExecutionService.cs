using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.Helpers;
using PuddleJobs.ApiService.Models;
using Quartz;

namespace PuddleJobs.ApiService.Services;

public interface IJobExecutionService
{
    Task ExecuteJobAsync(IJobExecutionContext context);
}

public class JobExecutionService : IJobExecutionService
{
    private readonly JobSchedulerDbContext _context;
    private readonly ILogger<JobExecutionService> _logger;
    private readonly IAssemblyStorageService _assemblyStorageService;

    public JobExecutionService(JobSchedulerDbContext context, ILogger<JobExecutionService> logger, IAssemblyStorageService assemblyStorageService)
    {
        _context = context;
        _logger = logger;
        _assemblyStorageService = assemblyStorageService;
    }

    public async Task ExecuteJobAsync(IJobExecutionContext context)
    {
        var jobData = context.JobDetail.JobDataMap;
        var jobId = jobData.GetInt("jobId");

        try
        {
            var job = await _context.Jobs
                .Include(j => j.Assembly)
                .ThenInclude(a => a.Versions)
                .FirstOrDefaultAsync(j => j.Id == jobId) 
                ?? throw new InvalidOperationException($"Job with ID {jobId} not found.");

            var activeAssembly = job.Assembly.ActiveVersion;
            var parameters = await LoadJobParametersAsync(jobId);
            foreach (var param in parameters)
            {
                jobData[param.Key] = param.Value;
            }

            await ExecuteJobFromAssemblyAsync(activeAssembly, context);
            
            _logger.LogInformation("Job {JobId} executed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing job {JobId}", jobId);
            throw;
        }
    }

    private async Task<Dictionary<string, object?>> LoadJobParametersAsync(int jobId)
    {
        var result = new Dictionary<string, object?>();

        var job = await _context.Jobs
            .Include(j => j.Parameters)
            .Include(j => j.Assembly)
                .ThenInclude(a => a.Versions)
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

        _logger.LogInformation("Loaded {ParameterCount} parameters for job {JobId}", result.Count, jobId);
        return result;
    }

    private async Task ExecuteJobFromAssemblyAsync(AssemblyVersion activeAssembly, IJobExecutionContext context)
    {
        var assembly = await _assemblyStorageService.LoadAssemblyVersionAsync(activeAssembly);

        var jobType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IJob).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface) 
            ?? throw new InvalidOperationException($"No job type implementing Quartz.IJob found in assembly '{activeAssembly.MainAssemblyName}'.");

        var jobInstance = (IJob?)Activator.CreateInstance(jobType) 
            ?? throw new InvalidOperationException($"Failed to create instance of job type '{jobType.Name}'.");

        await jobInstance.Execute(context);
    }
} 