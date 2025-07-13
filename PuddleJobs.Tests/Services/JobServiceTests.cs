using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.DTOs;
using PuddleJobs.ApiService.Helpers;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;
using PuddleJobs.Tests.TestHelpers;

namespace PuddleJobs.Tests.Services;

public class JobServiceTests
{
    private readonly Mock<IJobSchedulerService> _mockJobSchedulerService;
    private readonly Mock<IJobParameterService> _mockJobParameterService;
    private readonly Mock<ILogger<JobService>> _mockLogger;
    private readonly JobService _jobService;

    public JobServiceTests()
    {
        _mockJobSchedulerService = new Mock<IJobSchedulerService>();
        _mockJobParameterService = new Mock<IJobParameterService>();
        _mockLogger = new Mock<ILogger<JobService>>();
    }

    private JobSchedulerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new JobSchedulerDbContext(options);
    }

    private JobService CreateJobService(JobSchedulerDbContext context)
    {
        return new JobService(
            context,
            _mockJobSchedulerService.Object,
            _mockJobParameterService.Object,
            _mockLogger.Object
        );
    }

    #region GetAllJobsAsync Tests

    [Fact]
    public async Task GetAllJobsAsync_ReturnsAllJobs_WhenJobsExist()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        // Add assembly to context (jobs reference this by ID)
        var assembly = new Assembly
        {
            Id = 1,
            Name = "Test Assembly",
            Versions = new List<AssemblyVersion>
            {
                new() { Id = 1, Version = "1.0.0", IsActive = true }
            }
        };

        context.Assemblies.Add(assembly);

        await context.SaveChangesAsync();

        var jobs = new List<Job>
        {
            CreateTestJob(1, "Job 1"),
            CreateTestJob(2, "Job 2")
        };

        context.Jobs.Add(jobs[0]);
        context.Jobs.Add(jobs[1]);

        await context.SaveChangesAsync();

        // Act
        var result = await jobService.GetAllJobsAsync();
        var jobDtos = result.OrderBy(j => j.Id).ToList();

        // Assert
        Assert.NotNull(jobDtos);
        Assert.Equal(2, jobDtos.Count);
        Assert.Collection(jobDtos,
            job => Assert.Equal("Job 1", job.Name),
            job => Assert.Equal("Job 2", job.Name));
    }

    [Fact]
    public async Task GetAllJobsAsync_ReturnsEmptyList_WhenNoJobsExist()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);

        // Act
        var result = await jobService.GetAllJobsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetJobByIdAsync Tests

    [Fact]
    public async Task GetJobByIdAsync_ReturnsJob_WhenJobExists()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        // Add assembly to context (jobs reference this by ID)
        var assembly = new Assembly
        {
            Id = 1,
            Name = "Test Assembly",
            Versions = new List<AssemblyVersion>
            {
                new() { Id = 1, Version = "1.0.0", IsActive = true }
            }
        };
        context.Assemblies.Add(assembly);
        
        var job = CreateTestJob(1, "Test Job");
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        var result = await jobService.GetJobByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Job", result.Name);
    }

    [Fact]
    public async Task GetJobByIdAsync_ReturnsNull_WhenJobDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);

        // Act
        var result = await jobService.GetJobByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateJobAsync Tests

    [Fact]
    public async Task CreateJobAsync_CreatesJobSuccessfully_WhenValidDataProvided()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        var createDto = TestDataBuilder.CreateCreateJobDto();
        
        // Add assembly to context
        var assembly = new Assembly
        {
            Id = createDto.AssemblyId,
            Name = "Test Assembly",
            Versions = new List<AssemblyVersion>
            {
                new() { Id = 1, Version = "1.0.0", IsActive = true }
            }
        };
        context.Assemblies.Add(assembly);

        // Add schedules to context (jobs reference these by ID)
        var schedules = new List<Schedule>
        {
            new() { Id = 1, Name = "Schedule 1", CronExpression = "0 0 * * * ?", IsActive = true },
            new() { Id = 2, Name = "Schedule 2", CronExpression = "0 30 * * * ?", IsActive = true }
        };

        context.Schedules.AddRange(schedules);
        
        await context.SaveChangesAsync();

        _mockJobParameterService.Setup(x => x.ValidateJobParametersAsync(createDto.AssemblyId, createDto.Parameters))
            .Returns(Task.CompletedTask);

        _mockJobSchedulerService.Setup(x => x.UpdateJobAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await jobService.CreateJobAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.Name, result.Name);
        Assert.Equal(createDto.AssemblyId, result.AssemblyId);
        Assert.True(result.IsActive);

        // Verify job schedules were created
        var jobSchedules = await context.JobSchedules
            .Where(js => js.JobId == result.Id)
            .ToListAsync();

        Assert.Equal(createDto.ScheduleIds.Count, jobSchedules.Count);

        _mockJobParameterService.Verify(x => x.ValidateJobParametersAsync(createDto.AssemblyId, createDto.Parameters), Times.Once);
        _mockJobSchedulerService.Verify(x => x.UpdateJobAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task CreateJobAsync_ThrowsException_WhenParameterValidationFails()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        var createDto = TestDataBuilder.CreateCreateJobDto();

        _mockJobParameterService.Setup(x => x.ValidateJobParametersAsync(createDto.AssemblyId, createDto.Parameters))
            .ThrowsAsync(new InvalidOperationException("Invalid parameters"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => jobService.CreateJobAsync(createDto));

        Assert.Equal("Invalid parameters", exception.Message);
    }

    #endregion

    #region UpdateJobAsync Tests

    [Fact]
    public async Task UpdateJobAsync_UpdatesJobSuccessfully_WhenValidDataProvided()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        var jobId = 1;
        var updateDto = TestDataBuilder.CreateUpdateJobDto();
        var existingJob = CreateTestJob(jobId, "Original Name");
        
        // Add assembly to context
        var assembly = new Assembly
        {
            Id = existingJob.AssemblyId,
            Name = "Test Assembly",
            Versions = new List<AssemblyVersion>
            {
                new() { Id = 1, Version = "1.0.0", IsActive = true }
            }
        };
        context.Assemblies.Add(assembly);

        // Add schedules to context (jobs reference these by ID)
        var schedules = new List<Schedule>
        {
            new() { Id = 1, Name = "Schedule 1", CronExpression = "0 0 * * * ?", IsActive = true },
            new() { Id = 3, Name = "Schedule 3", CronExpression = "0 15 * * * ?", IsActive = true }
        };
        context.Schedules.AddRange(schedules);
        
        context.Jobs.Add(existingJob);
        await context.SaveChangesAsync();

        _mockJobParameterService.Setup(x => x.ValidateJobParametersAsync(existingJob.AssemblyId, updateDto.Parameters))
            .Returns(Task.CompletedTask);

        _mockJobParameterService.Setup(x => x.SetJobParameterValuesAsync(jobId, updateDto.Parameters))
            .ReturnsAsync(true);

        _mockJobSchedulerService.Setup(x => x.UpdateJobAsync(jobId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await jobService.UpdateJobAsync(jobId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.IsActive, result.IsActive);

        // Verify job schedules were updated
        var jobSchedules = await context.JobSchedules
            .Where(js => js.JobId == jobId)
            .ToListAsync();
        Assert.Equal(updateDto.ScheduleIds.Count, jobSchedules.Count);

        _mockJobParameterService.Verify(x => x.ValidateJobParametersAsync(existingJob.AssemblyId, updateDto.Parameters), Times.Once);
        _mockJobParameterService.Verify(x => x.SetJobParameterValuesAsync(jobId, updateDto.Parameters), Times.Once);
        _mockJobSchedulerService.Verify(x => x.UpdateJobAsync(jobId), Times.Once);
    }

    [Fact]
    public async Task UpdateJobAsync_ThrowsException_WhenJobNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        var jobId = 999;
        var updateDto = TestDataBuilder.CreateUpdateJobDto();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => jobService.UpdateJobAsync(jobId, updateDto));

        Assert.Equal("Job not found", exception.Message);
    }

    [Fact]
    public async Task UpdateJobAsync_ThrowsException_WhenParameterValidationFails()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        var jobId = 1;
        var updateDto = TestDataBuilder.CreateUpdateJobDto();
        var existingJob = CreateTestJob(jobId, "Original Name");
        
        // Add assembly to context
        var assembly = new Assembly
        {
            Id = existingJob.AssemblyId,
            Name = "Test Assembly",
            Versions = new List<AssemblyVersion>
            {
                new() { Id = 1, Version = "1.0.0", IsActive = true }
            }
        };
        context.Assemblies.Add(assembly);

        // Add schedules to context (jobs reference these by ID)
        var schedules = new List<Schedule>
        {
            new() { Id = 1, Name = "Schedule 1", CronExpression = "0 0 * * * ?", IsActive = true },
            new() { Id = 3, Name = "Schedule 3", CronExpression = "0 15 * * * ?", IsActive = true }
        };
        context.Schedules.AddRange(schedules);
        
        context.Jobs.Add(existingJob);
        await context.SaveChangesAsync();

        _mockJobParameterService.Setup(x => x.ValidateJobParametersAsync(existingJob.AssemblyId, updateDto.Parameters))
            .ThrowsAsync(new InvalidOperationException("Invalid parameters"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => jobService.UpdateJobAsync(jobId, updateDto));

        Assert.Equal("Invalid parameters", exception.Message);
    }

    #endregion

    #region DeleteJobAsync Tests

    [Fact]
    public async Task DeleteJobAsync_DeletesJobSuccessfully_WhenJobExists()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        // Add assembly to context (jobs reference this by ID)
        var assembly = new Assembly
        {
            Id = 1,
            Name = "Test Assembly",
            Versions = new List<AssemblyVersion>
            {
                new() { Id = 1, Version = "1.0.0", IsActive = true }
            }
        };
        context.Assemblies.Add(assembly);
        
        var jobId = 1;
        var job = CreateTestJob(jobId, "Test Job");
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        _mockJobSchedulerService.Setup(x => x.DeleteJobAsync(jobId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await jobService.DeleteJobAsync(jobId);

        // Assert
        Assert.True(result);
        
        // Verify the job was soft deleted
        var deletedJob = await context.Jobs.FindAsync(jobId);
        Assert.True(deletedJob!.IsDeleted);
        Assert.NotNull(deletedJob.DeletedAt);

        _mockJobSchedulerService.Verify(x => x.DeleteJobAsync(jobId), Times.Once);
    }

    [Fact]
    public async Task DeleteJobAsync_ReturnsFalse_WhenJobDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        var jobId = 999;

        // Act
        var result = await jobService.DeleteJobAsync(jobId);

        // Assert
        Assert.False(result);
        _mockJobSchedulerService.Verify(x => x.DeleteJobAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region GetJobParametersAsync Tests

    [Fact]
    public async Task GetJobParametersAsync_ReturnsParameters_WhenJobExists()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        var jobId = 1;
        var job = CreateTestJob(jobId, "Test Job");
        var assembly = new Assembly
        {
            Id = 1,
            Name = "Test Assembly",
            Versions = new List<AssemblyVersion>
            {
                new() { Id = 1, Version = "1.0.0", IsActive = true }
            }
        };
        job.Assembly = assembly;
        
        var parameterDefinitions = new List<AssemblyParameterDefinition>
        {
            new() { Id = 1, Name = "Param1", Type = "System.String", Required = true, AssemblyVersionId = 1 },
            new() { Id = 2, Name = "Param2", Type = "System.Int32", Required = false, DefaultValue = "42", AssemblyVersionId = 1 }
        };

        context.Assemblies.Add(assembly);
        context.Jobs.Add(job);
        context.AssemblyParameterDefinitions.AddRange(parameterDefinitions);
        await context.SaveChangesAsync();

        // Act
        var result = await jobService.GetJobParametersAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("Param1", result[0].Name);
        Assert.Equal("System.String", result[0].Type);
        Assert.True(result[0].Required);
        Assert.Equal("Param2", result[1].Name);
        Assert.Equal("System.Int32", result[1].Type);
        Assert.False(result[1].Required);
        Assert.Equal("42", result[1].DefaultValue);
    }

    [Fact]
    public async Task GetJobParametersAsync_ThrowsException_WhenJobDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var jobService = CreateJobService(context);
        
        var jobId = 999;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => jobService.GetJobParametersAsync(jobId));

        Assert.Equal($"Job with ID {jobId} not found.", exception.Message);
    }

    #endregion

    #region Helper Methods

    private static Job CreateTestJob(int id, string name, bool isActive = true)
    {
        return new Job
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            IsActive = isActive,
            AssemblyId = 1,
            CreatedAt = DateTime.UtcNow,
            JobSchedules = new List<JobSchedule>(),
            Parameters = new List<JobParameter>()
        };
    }

    #endregion
} 