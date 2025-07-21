using Microsoft.EntityFrameworkCore;
using Moq;
using PuddleJobs.ApiService.Data;
using PuddleJobs.Core.DTOs;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;
using PuddleJobs.Tests.TestHelpers;

namespace PuddleJobs.Tests.Services;

public class ScheduleServiceTests
{
    private readonly Mock<ICronValidationService> _mockCronValidationService;
    private readonly Mock<IJobSchedulerService> _mockJobSchedulerService;

    public ScheduleServiceTests()
    {
        _mockCronValidationService = new Mock<ICronValidationService>();
        _mockJobSchedulerService = new Mock<IJobSchedulerService>();
    }

    private JobSchedulerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new JobSchedulerDbContext(options);
    }

    private ScheduleService CreateScheduleService(JobSchedulerDbContext context)
    {
        return new ScheduleService(
            context,
            _mockCronValidationService.Object,
            _mockJobSchedulerService.Object
        );
    }

    #region GetAllSchedulesAsync Tests

    [Fact]
    public async Task GetAllSchedulesAsync_ReturnsAllSchedules_WhenSchedulesExist()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var schedules = new List<Schedule>
        {
            CreateTestSchedule(1, "Schedule 1"),
            CreateTestSchedule(2, "Schedule 2")
        };

        context.Schedules.AddRange(schedules);
        await context.SaveChangesAsync();

        // Act
        var result = await scheduleService.GetAllSchedulesAsync();
        var scheduleDtos = result.OrderBy(s => s.Id).ToList();

        // Assert
        Assert.NotNull(scheduleDtos);
        Assert.Equal(2, scheduleDtos.Count);
        Assert.Collection(scheduleDtos,
            schedule => Assert.Equal("Schedule 1", schedule.Name),
            schedule => Assert.Equal("Schedule 2", schedule.Name));
    }

    [Fact]
    public async Task GetAllSchedulesAsync_ReturnsEmptyList_WhenNoSchedulesExist()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);

        // Act
        var result = await scheduleService.GetAllSchedulesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetScheduleByIdAsync Tests

    [Fact]
    public async Task GetScheduleByIdAsync_ReturnsSchedule_WhenScheduleExists()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var schedule = CreateTestSchedule(1, "Test Schedule");
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        // Act
        var result = await scheduleService.GetScheduleByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Schedule", result.Name);
    }

    [Fact]
    public async Task GetScheduleByIdAsync_ReturnsNull_WhenScheduleDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);

        // Act
        var result = await scheduleService.GetScheduleByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateScheduleAsync Tests

    [Fact]
    public async Task CreateScheduleAsync_CreatesScheduleSuccessfully_WhenValidDataProvided()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var createDto = TestDataBuilder.CreateCreateScheduleDto();
        
        _mockCronValidationService.Setup(x => x.ValidateCronExpression(createDto.CronExpression))
            .Returns(TestDataBuilder.CreateCronValidationResult(true));

        // Act
        var result = await scheduleService.CreateScheduleAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.Name, result.Name);
        Assert.Equal(createDto.Description, result.Description);
        Assert.Equal(createDto.CronExpression, result.CronExpression);
        Assert.True(result.IsActive);

        // Verify schedule was saved to database
        var savedSchedule = await context.Schedules.FindAsync(result.Id);
        Assert.NotNull(savedSchedule);
        Assert.Equal(createDto.Name, savedSchedule.Name);

        _mockCronValidationService.Verify(x => x.ValidateCronExpression(createDto.CronExpression), Times.Once);
    }

    [Fact]
    public async Task CreateScheduleAsync_ThrowsException_WhenCronValidationFails()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var createDto = TestDataBuilder.CreateCreateScheduleDto();

        _mockCronValidationService.Setup(x => x.ValidateCronExpression(createDto.CronExpression))
            .Returns(TestDataBuilder.CreateCronValidationResult(false, "Invalid cron expression"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scheduleService.CreateScheduleAsync(createDto));

        Assert.Equal("Invalid cron expression", exception.Message);
    }

    #endregion

    #region UpdateScheduleAsync Tests

    [Fact]
    public async Task UpdateScheduleAsync_UpdatesScheduleSuccessfully_WhenValidDataProvided()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 1;
        var updateDto = TestDataBuilder.CreateUpdateScheduleDto();
        var existingSchedule = CreateTestSchedule(scheduleId, "Original Name");
        
        context.Schedules.Add(existingSchedule);
        await context.SaveChangesAsync();

        _mockCronValidationService.Setup(x => x.ValidateCronExpression(updateDto.CronExpression!))
            .Returns(TestDataBuilder.CreateCronValidationResult(true));

        _mockJobSchedulerService.Setup(x => x.UpdateScheduleAsync(scheduleId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await scheduleService.UpdateScheduleAsync(scheduleId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Description, result.Description);
        Assert.Equal(updateDto.CronExpression, result.CronExpression);
        Assert.Equal(updateDto.IsActive, result.IsActive);

        // Verify schedule was updated in database
        var updatedSchedule = await context.Schedules.FindAsync(scheduleId);
        Assert.NotNull(updatedSchedule);
        Assert.Equal(updateDto.Description, updatedSchedule.Description);

        _mockCronValidationService.Verify(x => x.ValidateCronExpression(updateDto.CronExpression!), Times.Once);
        _mockJobSchedulerService.Verify(x => x.UpdateScheduleAsync(scheduleId), Times.Once);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ThrowsException_WhenScheduleNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 999;
        var updateDto = TestDataBuilder.CreateUpdateScheduleDto();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scheduleService.UpdateScheduleAsync(scheduleId, updateDto));

        Assert.Equal("Schedule not found", exception.Message);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ThrowsException_WhenCronValidationFails()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 1;
        var updateDto = TestDataBuilder.CreateUpdateScheduleDto();
        var existingSchedule = CreateTestSchedule(scheduleId, "Original Name");
        
        context.Schedules.Add(existingSchedule);
        await context.SaveChangesAsync();

        _mockCronValidationService.Setup(x => x.ValidateCronExpression(updateDto.CronExpression))
            .Returns(TestDataBuilder.CreateCronValidationResult(false, "Invalid cron expression"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scheduleService.UpdateScheduleAsync(scheduleId, updateDto));

        Assert.Equal("Invalid cron expression", exception.Message);
    }

    [Fact]
    public async Task UpdateScheduleAsync_UpdatesOnlyDescription_WhenOnlyDescriptionProvided()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 1;
        var updateDto = new UpdateScheduleDto
        {
            Description = "Updated Description"
            // CronExpression and IsActive are null
        };
        var existingSchedule = CreateTestSchedule(scheduleId, "Original Name");
        
        context.Schedules.Add(existingSchedule);
        await context.SaveChangesAsync();

        _mockJobSchedulerService.Setup(x => x.UpdateScheduleAsync(scheduleId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await scheduleService.UpdateScheduleAsync(scheduleId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Description, result.Description);
        Assert.Equal(existingSchedule.CronExpression, result.CronExpression); // Should remain unchanged
        Assert.Equal(existingSchedule.IsActive, result.IsActive); // Should remain unchanged

        _mockCronValidationService.Verify(x => x.ValidateCronExpression(It.IsAny<string>()), Times.Never);
        _mockJobSchedulerService.Verify(x => x.UpdateScheduleAsync(scheduleId), Times.Once);
    }

    #endregion

    #region DeleteScheduleAsync Tests

    [Fact]
    public async Task DeleteScheduleAsync_DeletesScheduleSuccessfully_WhenScheduleExists()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 1;
        var schedule = CreateTestSchedule(scheduleId, "Test Schedule");
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        _mockJobSchedulerService.Setup(x => x.DeleteScheduleAsync(scheduleId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await scheduleService.DeleteScheduleAsync(scheduleId);

        // Assert
        Assert.True(result);
        
        // Verify the schedule was soft deleted
        var deletedSchedule = await context.Schedules.FindAsync(scheduleId);
        Assert.True(deletedSchedule!.IsDeleted);
        Assert.NotNull(deletedSchedule.DeletedAt);

        _mockJobSchedulerService.Verify(x => x.DeleteScheduleAsync(scheduleId), Times.Once);
    }

    [Fact]
    public async Task DeleteScheduleAsync_ReturnsFalse_WhenScheduleDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 999;

        // Act
        var result = await scheduleService.DeleteScheduleAsync(scheduleId);

        // Assert
        Assert.False(result);
        _mockJobSchedulerService.Verify(x => x.DeleteScheduleAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region GetNextExecutionTimes Tests

    [Fact]
    public void GetNextExecutionTimes_ReturnsExecutionTimes_WhenScheduleExists()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 1;
        var schedule = CreateTestSchedule(scheduleId, "Test Schedule");
        context.Schedules.Add(schedule);
        context.SaveChanges();

        var expectedTimes = TestDataBuilder.CreateNextExecutionTimes(3);
        _mockCronValidationService.Setup(x => x.GetNextExecutionTimes(schedule.CronExpression, 3))
            .Returns(expectedTimes);

        // Act
        var result = scheduleService.GetNextExecutionTimes(scheduleId, 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        Assert.Equal(expectedTimes, result);

        _mockCronValidationService.Verify(x => x.GetNextExecutionTimes(schedule.CronExpression, 3), Times.Once);
    }

    [Fact]
    public void GetNextExecutionTimes_ReturnsEmptyEnumerable_WhenScheduleDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 999;

        // Act
        var result = scheduleService.GetNextExecutionTimes(scheduleId, 5);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockCronValidationService.Verify(x => x.GetNextExecutionTimes(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void GetNextExecutionTimes_UsesDefaultCount_WhenCountNotSpecified()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 1;
        var schedule = CreateTestSchedule(scheduleId, "Test Schedule");
        context.Schedules.Add(schedule);
        context.SaveChanges();

        var expectedTimes = TestDataBuilder.CreateNextExecutionTimes(5);
        _mockCronValidationService.Setup(x => x.GetNextExecutionTimes(schedule.CronExpression, 5))
            .Returns(expectedTimes);

        // Act
        var result = scheduleService.GetNextExecutionTimes(scheduleId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count());

        _mockCronValidationService.Verify(x => x.GetNextExecutionTimes(schedule.CronExpression, 5), Times.Once);
    }

    #endregion

    #region ValidateCronExpression Tests

    [Fact]
    public void ValidateCronExpression_ReturnsValidationResult_WhenCalled()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var cronExpression = "0 0 * * * ?";
        var expectedResult = TestDataBuilder.CreateCronValidationResult(true);
        
        _mockCronValidationService.Setup(x => x.ValidateCronExpression(cronExpression))
            .Returns(expectedResult);

        // Act
        var result = scheduleService.ValidateCronExpression(cronExpression);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockCronValidationService.Verify(x => x.ValidateCronExpression(cronExpression), Times.Once);
    }

    #endregion

    #region PauseScheduleAsync Tests

    [Fact]
    public async Task PauseScheduleAsync_PausesScheduleSuccessfully_WhenScheduleExists()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 1;
        var schedule = CreateTestSchedule(scheduleId, "Test Schedule");
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        _mockJobSchedulerService.Setup(x => x.PauseScheduleAsync(scheduleId))
            .Returns(Task.CompletedTask);

        // Act
        await scheduleService.PauseScheduleAsync(scheduleId);

        // Assert
        _mockJobSchedulerService.Verify(x => x.PauseScheduleAsync(scheduleId), Times.Once);
    }

    [Fact]
    public async Task PauseScheduleAsync_ThrowsException_WhenScheduleDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 999;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scheduleService.PauseScheduleAsync(scheduleId));

        Assert.Equal($"Schedule with ID {scheduleId} not found.", exception.Message);
        _mockJobSchedulerService.Verify(x => x.PauseScheduleAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region ResumeScheduleAsync Tests

    [Fact]
    public async Task ResumeScheduleAsync_ResumesScheduleSuccessfully_WhenScheduleExists()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 1;
        var schedule = CreateTestSchedule(scheduleId, "Test Schedule");
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        _mockJobSchedulerService.Setup(x => x.ResumeScheduleAsync(scheduleId))
            .Returns(Task.CompletedTask);

        // Act
        await scheduleService.ResumeScheduleAsync(scheduleId);

        // Assert
        _mockJobSchedulerService.Verify(x => x.ResumeScheduleAsync(scheduleId), Times.Once);
    }

    [Fact]
    public async Task ResumeScheduleAsync_ThrowsException_WhenScheduleDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var scheduleService = CreateScheduleService(context);
        
        var scheduleId = 999;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scheduleService.ResumeScheduleAsync(scheduleId));

        Assert.Equal($"Schedule with ID {scheduleId} not found.", exception.Message);
        _mockJobSchedulerService.Verify(x => x.ResumeScheduleAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private static Schedule CreateTestSchedule(int id, string name, bool isActive = true)
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

    #endregion
} 