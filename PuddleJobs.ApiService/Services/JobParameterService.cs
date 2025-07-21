using Microsoft.EntityFrameworkCore;
using PuddleJobs.ApiService.Data;
using PuddleJobs.Core.DTOs;
using PuddleJobs.ApiService.Helpers;
using PuddleJobs.ApiService.Models;

namespace PuddleJobs.ApiService.Services;

public interface IJobParameterService
{
    Task<IEnumerable<JobParameterDto>> GetJobParameterValuesAsync(int jobId);
    Task ValidateJobParametersAsync(int assemblyId, IEnumerable<JobParameterValueDto> parameters);
    Task<bool> SetJobParameterValuesAsync(int jobId, IEnumerable<JobParameterValueDto> parameters);
}

public class JobParameterService : IJobParameterService
{
    private readonly JobSchedulerDbContext _context;

    public JobParameterService(JobSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<JobParameterDto>> GetJobParameterValuesAsync(int jobId)
    {
        var job = await _context.Jobs.FindAsync(jobId) 
            ?? throw new InvalidOperationException($"Job with ID {jobId} not found.");

        return _context.JobParameters
            .Where(p => p.JobId == jobId)
            .Select(JobParameter.CreateDto);
    }

    public async Task<bool> SetJobParameterValuesAsync(int jobId, IEnumerable<JobParameterValueDto> parameters)
    {
        var job = await _context.Jobs.FindAsync(jobId)
            ?? throw new InvalidOperationException($"Job with ID {jobId} not found.");

        await ValidateJobParametersAsync(job.AssemblyId, parameters);

        var existingParameters = _context.JobParameters.Where(p => p.JobId == jobId).ToDictionary(x => x.Name);
        var updatedParameters = parameters.ToDictionary(x => x.Name);

        foreach (var updatedParameter in updatedParameters.Values)
        {
            if (existingParameters.TryGetValue(updatedParameter.Name, out var existingParameter))
            {
                existingParameter.Value = updatedParameter.Value;
                existingParameter.UpdatedAt = DateTime.UtcNow;
            } 
            else
            {
                var newParameter = new JobParameter
                {
                    Name = updatedParameter.Name,
                    Value = updatedParameter.Value,
                    CreatedAt = DateTime.UtcNow
                };

                job.Parameters.Add(newParameter);
            }
        }

        var oldParameterKeys = existingParameters.Keys.Except(updatedParameters.Keys);
        foreach(var oldParameterKey in oldParameterKeys)
        {
            var oldParameter = existingParameters[oldParameterKey];
            oldParameter.Value = null;
            oldParameter.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ValidateJobParametersAsync(int assemblyId, IEnumerable<JobParameterValueDto> parameters)
    {
        // Get the assembly with its active version
        var assembly = await _context.Assemblies
            .Include(a => a.Versions)
            .FirstOrDefaultAsync(a => a.Id == assemblyId) 
            ?? throw new InvalidOperationException($"Assembly with ID {assemblyId} not found.");

        if (assembly.ActiveVersion == null)
            throw new InvalidOperationException($"Assembly {assemblyId} has no active version.");

        // Get parameter definitions for the active version
        var parameterDefinitions = _context.AssemblyParameterDefinitions
            .Where(pd => pd.AssemblyVersionId == assembly.ActiveVersion.Id)
            .ToDictionary(pd => pd.Name, pd => pd);

        var parameterDict = parameters.ToDictionary(p => p.Name, p => p);

        // Validate each parameter definition
        foreach (var definition in parameterDefinitions.Values)
        {
            if (definition.Required && !parameterDict.ContainsKey(definition.Name))
            {
                throw new InvalidOperationException($"Required parameter '{definition.Name}' is missing.");
            }

            if (parameterDict.TryGetValue(definition.Name, out var providedParam))
            {
                // Validate parameter value if provided
                if (!string.IsNullOrEmpty(providedParam.Value))
                {
                    JobParameterHelper.ConvertJobParameterValue(providedParam.Value, definition.Type);
                }
                else if (definition.Required)
                {
                    throw new InvalidOperationException($"Required parameter '{definition.Name}' cannot be null or empty.");
                }
            }
        }

        // Check for extra parameters that aren't defined
        var extraParameters = parameterDict.Keys.Except(parameterDefinitions.Keys);
        if (extraParameters.Any())
        {
            throw new InvalidOperationException($"Unknown parameters provided: {string.Join(", ", extraParameters)}");
        }
    }
} 