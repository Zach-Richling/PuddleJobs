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
}

public class JobService : IJobService
{
    private readonly JobSchedulerDbContext _context;
    private readonly IJobSchedulerService _jobSchedulerService;
    private readonly IJobParameterService _jobParameterService;

    public JobService(JobSchedulerDbContext context, IJobSchedulerService jobSchedulerService, IJobParameterService jobParameterService)
    {
        _context = context;
        _jobSchedulerService = jobSchedulerService;
        _jobParameterService = jobParameterService;
    }

    public async Task<IEnumerable<JobDto>> GetAllJobsAsync()
    {
        var jobs = _context.Jobs
            .Include(j => j.Assembly)
                .ThenInclude(a => a.Versions)
            .Include(j => j.JobSchedules)
                .ThenInclude(js => js.Schedule)
            .AsSplitQuery();

        return await Task.FromResult(jobs.Select(JobDto.Create));
    }

    public async Task<JobDto?> GetJobByIdAsync(int id)
    {
        var job = _context.Jobs
            .Include(j => j.Assembly)
                .ThenInclude(a => a.Versions)
            .Include(j => j.JobSchedules)
                .ThenInclude(js => js.Schedule)
            .AsSplitQuery()
            .FirstOrDefault(j => j.Id == id);

        return await Task.FromResult(job == null ? null : JobDto.Create(job));
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
} 