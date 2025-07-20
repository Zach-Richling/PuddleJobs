using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.Helpers;
using PuddleJobs.ApiService.Models;
using Quartz;
using Quartz.Impl.Matchers;

namespace PuddleJobs.ApiService.Services;

public interface IJobSchedulerService
{
    Task InitializeSchedulerAsync();

    Task UpdateJobAsync(int jobId);
    Task DeleteJobAsync(int jobId);
    Task PauseJobAsync(int jobId);
    Task ResumeJobAsync(int jobId);
    
    Task UpdateScheduleAsync(int scheduleId);
    Task DeleteScheduleAsync(int scheduleId);
    Task PauseScheduleAsync(int scheduleId);
    Task ResumeScheduleAsync(int scheduleId);
}

public class JobSchedulerService : IJobSchedulerService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly JobSchedulerDbContext _context;
    private readonly ILogger<JobSchedulerService> _logger;

    public JobSchedulerService(
        ISchedulerFactory schedulerFactory,
        JobSchedulerDbContext context,
        ILogger<JobSchedulerService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _context = context;
        _logger = logger;
    }

    public async Task InitializeSchedulerAsync()
    {
        _logger.LogInformation("Initializing job scheduler...");

        var scheduler = await _schedulerFactory.GetScheduler();
        
        // Clear existing jobs
        await scheduler.Clear();

        // Load all active job schedules from database
        var jobSchedules = _context.JobSchedules
            .Include(js => js.Job)
            .Include(js => js.Schedule)
            .Include(js => js.Job.Assembly)
                .ThenInclude(a => a.Versions)
            .Where(js => js.Job.IsActive && js.Schedule.IsActive)
            .AsSplitQuery()
            .ToList();

        _logger.LogInformation("Found {JobScheduleCount} active job schedules", jobSchedules.Count);

        // Create Quartz jobs and triggers
        foreach (var jobSchedule in jobSchedules)
        {
            try
            {
                await CreateQuartzJobAsync(jobSchedule);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to create Quartz job for job schedule {JobScheduleId}", jobSchedule.Id);
            }
        }

        _logger.LogInformation("Job scheduler initialization completed");
    }

    public async Task UpdateJobAsync(int jobId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        
        await scheduler.DeleteJob(Job.GetJobKey(jobId));

        var jobSchedules = _context.JobSchedules
            .Include(js => js.Job)
            .Include(js => js.Schedule)
            .Include(js => js.Job.Assembly)
                .ThenInclude(a => a.Versions)
            .Where(js => js.JobId == jobId && js.Job.IsActive && js.Schedule.IsActive)
            .AsSplitQuery();

        foreach (var jobSchedule in jobSchedules)
        {
            await CreateQuartzJobAsync(jobSchedule);
        }
    }

    public async Task DeleteJobAsync(int jobId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = Job.GetJobKey(jobId);
        await scheduler.DeleteJob(jobKey);
    }

    public async Task PauseJobAsync(int jobId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = Job.GetJobKey(jobId);
        await scheduler.PauseJob(jobKey);
    }

    public async Task ResumeJobAsync(int jobId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = Job.GetJobKey(jobId);
        await scheduler.ResumeJob(jobKey);
    }

    public async Task UpdateScheduleAsync(int scheduleId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        
        var jobSchedules = _context.JobSchedules
            .Include(js => js.Job)
            .Include(js => js.Schedule)
                .Where(js => js.ScheduleId == scheduleId && js.Job.IsActive && js.Schedule.IsActive)
            .ToList();

        await DeleteScheduleAsync(scheduleId);

        foreach (var jobSchedule in jobSchedules)
        {
            await CreateQuartzJobAsync(jobSchedule);
        }
    }

    public async Task DeleteScheduleAsync(int scheduleId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var triggers = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(scheduleId.ToString()));
        await scheduler.UnscheduleJobs(triggers);
    }

    public async Task PauseScheduleAsync(int scheduleId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.PauseTriggers(GroupMatcher<TriggerKey>.GroupEquals(scheduleId.ToString()));
    }

    public async Task ResumeScheduleAsync(int scheduleId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.ResumeTriggers(GroupMatcher<TriggerKey>.GroupEquals(scheduleId.ToString()));
    }

    public async Task CreateQuartzJobAsync(JobSchedule jobSchedule)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        
        var job = jobSchedule.Job;
        var schedule = jobSchedule.Schedule;

        var jobKey = job.JobKey;
        var triggerKey = jobSchedule.TriggerKey;

        // Check if job already exists
        var existingJob = await scheduler.GetJobDetail(jobKey);
        if (existingJob == null)
        {
            // Create job metadata
            var jobMetaData = new JobDataMap
            {
                ["jobId"] = job.Id,
            };

            // Create Quartz job
            var quartzJob = JobBuilder.Create<PuddleJob>()
                .WithIdentity(jobKey)
                .UsingJobData(jobMetaData)
                .StoreDurably()
                .Build();

            await scheduler.AddJob(quartzJob, true);
            _logger.LogInformation("Created quartz job {JobId}", job.Id);
        }

        var trigger = TriggerBuilder.Create()
               .WithIdentity(triggerKey)
               .ForJob(jobKey)
               .WithCronSchedule(schedule.CronExpression)
               .Build();

        await scheduler.ScheduleJob(trigger);

        _logger.LogInformation("Scheduled quartz job for Job {JobId} with schedule {ScheduleId}", job.Id, schedule.Id);
    }
} 