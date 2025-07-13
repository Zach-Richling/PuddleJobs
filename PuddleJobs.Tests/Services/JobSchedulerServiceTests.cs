using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using System.Collections.Specialized;

namespace PuddleJobs.Tests.Services;

public class JobSchedulerServiceTests
{
    private readonly Mock<ILogger<JobSchedulerService>> _mockLogger;

    public JobSchedulerServiceTests()
    {
        _mockLogger = new Mock<ILogger<JobSchedulerService>>();
    }

    private JobSchedulerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new JobSchedulerDbContext(options);
    }

    private static ISchedulerFactory CreateSchedulerFactory()
    {
        var properties = new NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = $"TestScheduler_{Guid.NewGuid()}",
            ["quartz.scheduler.instanceId"] = "AUTO",
            ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz",
            ["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz",
            ["quartz.threadPool.threadCount"] = "1"
        };

        var factory = new StdSchedulerFactory(properties);
        return factory;
    }

    private JobSchedulerService CreateJobSchedulerService(JobSchedulerDbContext context, ISchedulerFactory schedulerFactory)
    {
        return new JobSchedulerService(schedulerFactory, context, _mockLogger.Object);
    }

    #region InitializeSchedulerAsync Tests

    [Fact]
    public async Task InitializeSchedulerAsync_ClearsExistingJobsAndCreatesNewOnes_WhenActiveJobSchedulesExist()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);

        await context.SaveChangesAsync();

        // Act
        await jobSchedulerService.InitializeSchedulerAsync();

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();

        Assert.True(await scheduler.CheckExists(job.JobKey));
        Assert.True(await scheduler.CheckExists(jobSchedule.TriggerKey));
        
        await scheduler.Shutdown();
    }

    [Fact]
    public async Task InitializeSchedulerAsync_DoesNotCreateJobs_WhenNoActiveJobSchedulesExist()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        // Act
        await jobSchedulerService.InitializeSchedulerAsync();

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Empty(jobKeys);
        Assert.Empty(triggerKeys);
        
        await scheduler.Shutdown();
    }

    [Fact]
    public async Task InitializeSchedulerAsync_DoesNotCreateJobs_WhenJobIsInactive()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, false); // Inactive job
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);
        await context.SaveChangesAsync();

        // Act
        await jobSchedulerService.InitializeSchedulerAsync();

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Empty(jobKeys);
        Assert.Empty(triggerKeys);
        
        await scheduler.Shutdown();
    }

    [Fact]
    public async Task InitializeSchedulerAsync_DoesNotCreateJobs_WhenScheduleIsInactive()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", false); // Inactive schedule
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);
        await context.SaveChangesAsync();

        // Act
        await jobSchedulerService.InitializeSchedulerAsync();

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Empty(jobKeys);
        Assert.Empty(triggerKeys);
        
        await scheduler.Shutdown();
    }

    #endregion

    #region UpdateJobAsync Tests

    [Fact]
    public async Task UpdateJobAsync_DeletesExistingJobAndCreatesNewOnes_WhenActiveJobSchedulesExist()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule1 = CreateTestSchedule(1, "Schedule 1", true);
        var schedule2 = CreateTestSchedule(2, "Schedule 2", true);
        var jobSchedule1 = CreateTestJobSchedule(1, job.Id, schedule1.Id);
        var jobSchedule2 = CreateTestJobSchedule(2, job.Id, schedule2.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.AddRange(schedule1, schedule2);
        context.JobSchedules.AddRange(jobSchedule1, jobSchedule2);
        
        await context.SaveChangesAsync();

        // Initialize scheduler first
        await jobSchedulerService.InitializeSchedulerAsync();

        // Act
        await jobSchedulerService.UpdateJobAsync(job.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Single(jobKeys);
        Assert.Equal(2, triggerKeys.Count);
        
        await scheduler.Shutdown();
    }

    [Fact]
    public async Task UpdateJobAsync_DoesNotCreateJobs_WhenNoActiveJobSchedulesExist()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var job = CreateTestJob(1, "Test Job", 1, true);
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        await jobSchedulerService.UpdateJobAsync(job.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Empty(jobKeys);
        Assert.Empty(triggerKeys);
        
        await scheduler.Shutdown();
    }

    #endregion

    #region DeleteJobAsync Tests

    [Fact]
    public async Task DeleteJobAsync_DeletesJobFromScheduler()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);
        await context.SaveChangesAsync();

        await jobSchedulerService.InitializeSchedulerAsync();

        // Act
        await jobSchedulerService.DeleteJobAsync(job.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Empty(jobKeys);
        Assert.Empty(triggerKeys);
        
        await scheduler.Shutdown();
    }

    #endregion

    #region PauseJobAsync Tests

    [Fact]
    public async Task PauseJobAsync_PausesJobInScheduler()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);
        await context.SaveChangesAsync();

        await jobSchedulerService.InitializeSchedulerAsync();

        // Act
        await jobSchedulerService.PauseJobAsync(job.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var triggers = await scheduler.GetTriggersOfJob(job.JobKey);
        foreach (var trigger in triggers)
        {
            var state = await scheduler.GetTriggerState(trigger.Key);
            Assert.Equal(TriggerState.Paused, state);
        }
        await scheduler.Shutdown();
    }

    #endregion

    #region ResumeJobAsync Tests

    [Fact]
    public async Task ResumeJobAsync_ResumesJobInScheduler()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);
        await context.SaveChangesAsync();

        await jobSchedulerService.InitializeSchedulerAsync();
        await jobSchedulerService.PauseJobAsync(job.Id);

        // Act
        await jobSchedulerService.ResumeJobAsync(job.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var triggers = await scheduler.GetTriggersOfJob(job.JobKey);
        foreach (var trigger in triggers)
        {
            var state = await scheduler.GetTriggerState(trigger.Key);
            Assert.Equal(TriggerState.Normal, state);
        }
        await scheduler.Shutdown();
    }

    #endregion

    #region UpdateScheduleAsync Tests

    [Fact]
    public async Task UpdateScheduleAsync_DeletesExistingTriggersAndCreatesNewOnes_WhenActiveJobSchedulesExist()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job1 = CreateTestJob(1, "Job 1", 1, true);
        var job2 = CreateTestJob(2, "Job 2", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule1 = CreateTestJobSchedule(1, job1.Id, schedule.Id);
        var jobSchedule2 = CreateTestJobSchedule(2, job2.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.AddRange(job1, job2);
        context.Schedules.Add(schedule);
        context.JobSchedules.AddRange(jobSchedule1, jobSchedule2);
        await context.SaveChangesAsync();

        await jobSchedulerService.InitializeSchedulerAsync();

        // Act
        await jobSchedulerService.UpdateScheduleAsync(schedule.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Equal(2, jobKeys.Count);
        Assert.Equal(2, triggerKeys.Count);
        
        await scheduler.Shutdown();
    }

    [Fact]
    public async Task UpdateScheduleAsync_DoesNotCreateJobs_WhenNoActiveJobSchedulesExist()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        // Act
        await jobSchedulerService.UpdateScheduleAsync(schedule.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Empty(jobKeys);
        Assert.Empty(triggerKeys);
        
        await scheduler.Shutdown();
    }

    #endregion

    #region DeleteScheduleAsync Tests

    [Fact]
    public async Task DeleteScheduleAsync_DeletesAllTriggersForSchedule()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);
        await context.SaveChangesAsync();

        await jobSchedulerService.InitializeSchedulerAsync();

        // Act
        await jobSchedulerService.DeleteScheduleAsync(schedule.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Single(jobKeys); // Job still exists
        Assert.Empty(triggerKeys); // But triggers are deleted
        
        await scheduler.Shutdown();
    }

    #endregion

    #region PauseScheduleAsync Tests

    [Fact]
    public async Task PauseScheduleAsync_PausesAllTriggersForSchedule()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);
        await context.SaveChangesAsync();

        await jobSchedulerService.InitializeSchedulerAsync();

        // Act
        await jobSchedulerService.PauseScheduleAsync(schedule.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(schedule.Id.ToString()));
        foreach (var triggerKey in triggerKeys)
        {
            var state = await scheduler.GetTriggerState(triggerKey);
            Assert.Equal(TriggerState.Paused, state);
        }
        await scheduler.Shutdown();
    }

    #endregion

    #region ResumeScheduleAsync Tests

    [Fact]
    public async Task ResumeScheduleAsync_ResumesAllTriggersForSchedule()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);
        await context.SaveChangesAsync();

        await jobSchedulerService.InitializeSchedulerAsync();
        await jobSchedulerService.PauseScheduleAsync(schedule.Id);

        // Act
        await jobSchedulerService.ResumeScheduleAsync(schedule.Id);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(schedule.Id.ToString()));
        foreach (var triggerKey in triggerKeys)
        {
            var state = await scheduler.GetTriggerState(triggerKey);
            Assert.Equal(TriggerState.Normal, state);
        }
        await scheduler.Shutdown();
    }

    #endregion

    #region CreateQuartzJobAsync Tests

    [Fact]
    public async Task CreateQuartzJobAsync_CreatesNewJob_WhenJobDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule = CreateTestSchedule(1, "Test Schedule", true);
        var jobSchedule = CreateTestJobSchedule(1, job.Id, schedule.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.Add(schedule);
        context.JobSchedules.Add(jobSchedule);
        await context.SaveChangesAsync();

        // Load the jobSchedule with all required navigation properties
        var loadedJobSchedule = context.JobSchedules
            .Include(js => js.Job)
            .Include(js => js.Schedule)
            .Include(js => js.Job.Assembly)
                .ThenInclude(a => a.Versions)
            .First(js => js.Id == jobSchedule.Id);

        // Act
        await jobSchedulerService.CreateQuartzJobAsync(loadedJobSchedule);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        Assert.Single(jobKeys);
        Assert.Single(triggerKeys);
        
        await scheduler.Shutdown();
    }

    [Fact]
    public async Task CreateQuartzJobAsync_DoesNotCreateJob_WhenJobAlreadyExists()
    {
        // Arrange
        using var context = CreateContext();
        var schedulerFactory = CreateSchedulerFactory();
        var jobSchedulerService = CreateJobSchedulerService(context, schedulerFactory);

        var assembly = CreateTestAssembly(1, "Test Assembly", false);
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", 1, true);
        assembly.Versions.Add(assemblyVersion);

        var job = CreateTestJob(1, "Test Job", 1, true);
        var schedule1 = CreateTestSchedule(1, "Schedule 1", true);
        var schedule2 = CreateTestSchedule(2, "Schedule 2", true);
        var jobSchedule1 = CreateTestJobSchedule(1, job.Id, schedule1.Id);
        var jobSchedule2 = CreateTestJobSchedule(2, job.Id, schedule2.Id);

        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.Jobs.Add(job);
        context.Schedules.AddRange(schedule1, schedule2);
        context.JobSchedules.AddRange(jobSchedule1, jobSchedule2);
        await context.SaveChangesAsync();

        // Load the jobSchedules with all required navigation properties
        var loadedJobSchedules = context.JobSchedules
            .Include(js => js.Job)
            .Include(js => js.Schedule)
            .Include(js => js.Job.Assembly)
                .ThenInclude(a => a.Versions)
            .Where(js => js.JobId == job.Id)
            .ToList();

        // Act - Create the first job schedule
        await jobSchedulerService.CreateQuartzJobAsync(loadedJobSchedules[0]);
        
        // Create the second job schedule (should reuse the same job)
        await jobSchedulerService.CreateQuartzJobAsync(loadedJobSchedules[1]);

        // Assert
        var scheduler = await schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
        
        // Should have 1 job (reused) and 2 triggers (one for each schedule)
        Assert.Single(jobKeys);
        Assert.Equal(2, triggerKeys.Count);
        
        await scheduler.Shutdown();
    }

    #endregion

    #region Helper Methods

    private static Job CreateTestJob(int id, string name, int assemblyId, bool isActive)
    {
        return new Job
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            IsActive = isActive,
            AssemblyId = assemblyId,
            CreatedAt = DateTime.UtcNow,
            JobSchedules = new List<JobSchedule>(),
            Parameters = new List<JobParameter>()
        };
    }

    private static Schedule CreateTestSchedule(int id, string name, bool isActive)
    {
        return new Schedule
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            CronExpression = "0 0 * * * ?",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            JobSchedules = new List<JobSchedule>()
        };
    }

    private static JobSchedule CreateTestJobSchedule(int id, int jobId, int scheduleId)
    {
        return new JobSchedule
        {
            Id = id,
            JobId = jobId,
            ScheduleId = scheduleId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Assembly CreateTestAssembly(int id, string name, bool isDeleted)
    {
        return new Assembly
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            CreatedAt = DateTime.UtcNow,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            Versions = new List<AssemblyVersion>(),
            Jobs = new List<Job>()
        };
    }

    private static AssemblyVersion CreateTestAssemblyVersion(int id, string version, int assemblyId, bool isActive)
    {
        return new AssemblyVersion
        {
            Id = id,
            Version = version,
            DirectoryPath = $"/test/path/{version}",
            MainAssemblyName = "TestingApp.dll",
            UploadedAt = DateTime.UtcNow,
            ChangeNotes = $"Change notes for {version}",
            IsActive = isActive,
            AssemblyId = assemblyId,
            ParameterDefinitions = new List<AssemblyParameterDefinition>()
        };
    }

    #endregion
} 