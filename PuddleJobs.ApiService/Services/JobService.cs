using Microsoft.EntityFrameworkCore;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.DTOs;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Helpers;

namespace PuddleJobs.ApiService.Services;

public interface IJobService
{
    Task<IEnumerable<JobDto>> GetAllJobsAsync();
    Task<JobDto?> GetJobByIdAsync(int id);
    Task<JobDto> CreateJobAsync(CreateJobDto dto);
    Task<JobDto> UpdateJobAsync(int id, UpdateJobDto dto);
    Task<bool> DeleteJobAsync(int id);
    Task<JobParameterInfo[]> GetJobParametersAsync(int id);
}

public class JobService : IJobService
{
    private readonly JobSchedulerDbContext _context;
    private readonly IJobSchedulerService _jobSchedulerService;
    private readonly IJobParameterService _jobParameterService;
    private readonly ILogger<JobService> _logger;

    public JobService(JobSchedulerDbContext context, IJobSchedulerService jobSchedulerService, IJobParameterService jobParameterService, ILogger<JobService> logger)
    {
        _context = context;
        _jobSchedulerService = jobSchedulerService;
        _jobParameterService = jobParameterService;
        _logger = logger;
    }

    public async Task<IEnumerable<JobDto>> GetAllJobsAsync()
    {
        var jobs = await _context.Jobs
            .Include(j => j.Assembly)
                .ThenInclude(a => a.Versions)
            .Include(j => j.JobSchedules)
                .ThenInclude(js => js.Schedule)
            .ToListAsync();
        return jobs.Select(JobDto.Create).ToList();
    }

    public async Task<JobDto?> GetJobByIdAsync(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Assembly)
                .ThenInclude(a => a.Versions)
            .Include(j => j.JobSchedules)
                .ThenInclude(js => js.Schedule)
            .FirstOrDefaultAsync(j => j.Id == id);

        return job == null ? null : JobDto.Create(job);
    }

    public async Task<JobDto> CreateJobAsync(CreateJobDto dto)
    {
        await _jobParameterService.ValidateJobParametersAsync(dto.AssemblyId, dto.Parameters);

        var job = new Job
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            AssemblyId = dto.AssemblyId
        };

        _context.Jobs.Add(job);

        foreach (var scheduleId in dto.ScheduleIds)
        {
            job.JobSchedules.Add(new JobSchedule
            {
                ScheduleId = scheduleId,
                CreatedAt = DateTime.UtcNow
            });
        }

        foreach (var parameter in dto.Parameters)
        {
            job.Parameters.Add(new JobParameter() 
            { 
                Name = parameter.Name,
                Value = parameter.Value,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        await _jobSchedulerService.UpdateJobAsync(job.Id);

        return await GetJobByIdAsync(job.Id) ?? throw new Exception("Job creation failed");
    }

    public async Task<JobDto> UpdateJobAsync(int id, UpdateJobDto dto)
    {
        var job = await _context.Jobs
            .Include(j => j.JobSchedules)
            .FirstOrDefaultAsync(j => j.Id == id) 
            ?? throw new InvalidOperationException("Job not found");

        // Validate parameters
        await _jobParameterService.ValidateJobParametersAsync(job.AssemblyId, dto.Parameters);

        if (!string.IsNullOrEmpty(dto.Name))
            job.Name = dto.Name;
        if (dto.IsActive.HasValue)
            job.IsActive = dto.IsActive.Value;

        job.Description = dto.Description;

        //Merge job schedules
        var existingScheduleIds = job.JobSchedules.Select(js => js.ScheduleId).ToHashSet();
        var newScheduleIds = dto.ScheduleIds;

        var schedulesToRemove = job.JobSchedules.Where(js => !newScheduleIds.Contains(js.ScheduleId));
        foreach (var js in schedulesToRemove)
        {
            _context.JobSchedules.Remove(js);
        }

        var schedulesToAdd = newScheduleIds.Where(scheduleId => !existingScheduleIds.Contains(scheduleId));
        foreach (var scheduleId in schedulesToAdd)
        {
            job.JobSchedules.Add(new JobSchedule
            {
                ScheduleId = scheduleId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _jobParameterService.SetJobParameterValuesAsync(job.Id, dto.Parameters);
        await _context.SaveChangesAsync();

        // Update Quartz jobs
        await _jobSchedulerService.UpdateJobAsync(job.Id);

        return await GetJobByIdAsync(job.Id) ?? throw new Exception("Job update failed");
    }

    public async Task<bool> DeleteJobAsync(int id)
    {
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return false;

        job.IsDeleted = true;
        job.DeletedAt = DateTime.UtcNow;

        // Delete Quartz jobs
        await _jobSchedulerService.DeleteJobAsync(job.Id);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<JobParameterInfo[]> GetJobParametersAsync(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Assembly)
            .ThenInclude(a => a.Versions)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            throw new InvalidOperationException($"Job with ID {id} not found.");

        // Get the active version for this assembly
        var activeVersion = job.Assembly.ActiveVersion;

        // Load parameter definitions from the database for the active version
        var parameterDefinitions = await _context.AssemblyParameterDefinitions
            .Where(pd => pd.AssemblyVersionId == activeVersion.Id)
            .ToListAsync();

        // Convert database parameter definitions to JobParameterInfo array
        return parameterDefinitions.Select(pd => new JobParameterInfo
        {
            Name = pd.Name,
            Type = pd.Type,
            Description = pd.Description,
            DefaultValue = pd.DefaultValue,
            Required = pd.Required
        }).ToArray();
    }
} 