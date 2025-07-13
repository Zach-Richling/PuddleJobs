using Microsoft.EntityFrameworkCore;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.DTOs;
using PuddleJobs.ApiService.Models;

namespace PuddleJobs.ApiService.Services;

public interface IScheduleService
{
    Task<IEnumerable<ScheduleDto>> GetAllSchedulesAsync();
    Task<ScheduleDto?> GetScheduleByIdAsync(int id);
    Task<ScheduleDto> CreateScheduleAsync(CreateScheduleDto dto);
    Task<ScheduleDto> UpdateScheduleAsync(int id, UpdateScheduleDto dto);
    Task<bool> DeleteScheduleAsync(int id);
    IEnumerable<DateTime> GetNextExecutionTimes(int scheduleId, int count = 5);
    CronValidationResult ValidateCronExpression(string cronExpression);
    Task PauseScheduleAsync(int scheduleId);
    Task ResumeScheduleAsync(int scheduleId);
}

public class ScheduleService : IScheduleService
{
    private readonly JobSchedulerDbContext _context;
    private readonly ICronValidationService _cronValidationService;
    private readonly IJobSchedulerService _jobSchedulerService;

    public ScheduleService(
        JobSchedulerDbContext context, 
        ICronValidationService cronValidationService,
        IJobSchedulerService jobSchedulerService)
    {
        _context = context;
        _cronValidationService = cronValidationService;
        _jobSchedulerService = jobSchedulerService;
    }

    public async Task<IEnumerable<ScheduleDto>> GetAllSchedulesAsync()
    {
        var schedules = await _context.Schedules
            .Include(s => s.JobSchedules)
            .ToListAsync();
        return schedules.Select(ScheduleDto.Create);
    }

    public async Task<ScheduleDto?> GetScheduleByIdAsync(int id)
    {
        var schedule = await _context.Schedules
            .Include(s => s.JobSchedules)
            .FirstOrDefaultAsync(s => s.Id == id);

        return schedule == null ? null : ScheduleDto.Create(schedule);
    }

    public async Task<ScheduleDto> CreateScheduleAsync(CreateScheduleDto dto)
    {
        Console.WriteLine("Got here");
        // Validate Cron expression
        var validationResult = _cronValidationService.ValidateCronExpression(dto.CronExpression);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(validationResult.ErrorMessage);
        }

        var schedule = new Schedule
        {
            Name = dto.Name,
            Description = dto.Description,
            CronExpression = dto.CronExpression,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        return await GetScheduleByIdAsync(schedule.Id) ?? throw new Exception("Schedule creation failed");
    }

    public async Task<ScheduleDto> UpdateScheduleAsync(int id, UpdateScheduleDto dto)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == id) 
            ?? throw new InvalidOperationException("Schedule not found");
        
        schedule.Description = dto.Description;

        if (dto.CronExpression != null)
        {
            // Validate Cron expression if provided
            var validationResult = _cronValidationService.ValidateCronExpression(dto.CronExpression);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }
            schedule.CronExpression = dto.CronExpression;
        }
        
        if (dto.IsActive.HasValue)
            schedule.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();

        // Update Quartz triggers for this schedule
        await _jobSchedulerService.UpdateScheduleAsync(schedule.Id);

        return await GetScheduleByIdAsync(schedule.Id) ?? throw new Exception("Schedule update failed");
    }

    public async Task<bool> DeleteScheduleAsync(int id)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == id);
        if (schedule == null)
            return false;

        // Delete Quartz triggers for this schedule before soft deleting
        await _jobSchedulerService.DeleteScheduleAsync(schedule.Id);

        schedule.IsDeleted = true;
        schedule.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public IEnumerable<DateTime> GetNextExecutionTimes(int scheduleId, int count = 5)
    {
        var schedule = _context.Schedules.FirstOrDefault(s => s.Id == scheduleId);
        if (schedule == null)
            return Enumerable.Empty<DateTime>();

        return _cronValidationService.GetNextExecutionTimes(schedule.CronExpression, count);
    }

    public CronValidationResult ValidateCronExpression(string cronExpression)
    {
        return _cronValidationService.ValidateCronExpression(cronExpression);
    }

    public async Task PauseScheduleAsync(int scheduleId)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule == null)
            throw new InvalidOperationException($"Schedule with ID {scheduleId} not found.");

        // Pause Quartz triggers for this schedule
        await _jobSchedulerService.PauseScheduleAsync(scheduleId);
    }

    public async Task ResumeScheduleAsync(int scheduleId)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule == null)
            throw new InvalidOperationException($"Schedule with ID {scheduleId} not found.");

        // Resume Quartz triggers for this schedule
        await _jobSchedulerService.ResumeScheduleAsync(scheduleId);
    }
} 