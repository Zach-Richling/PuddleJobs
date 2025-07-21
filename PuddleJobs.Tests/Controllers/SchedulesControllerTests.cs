using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Controllers;
using PuddleJobs.Core.DTOs;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;
using PuddleJobs.Tests.TestHelpers;
using Xunit;

namespace PuddleJobs.Tests.Controllers;

public class SchedulesControllerTests : ControllerTestBase
{
    private readonly Mock<IScheduleService> _mockScheduleService;
    private readonly Mock<ILogger<SchedulesController>> _mockLogger;
    private readonly SchedulesController _controller;

    public SchedulesControllerTests()
    {
        _mockScheduleService = new Mock<IScheduleService>();
        _mockLogger = new Mock<ILogger<SchedulesController>>();
        _controller = new SchedulesController(_mockScheduleService.Object);
    }

    #region GetSchedules Tests

    [Fact]
    public async Task GetSchedules_ReturnsOkResult_WithSchedules()
    {
        // Arrange
        var expectedSchedules = new List<ScheduleDto>
        {
            new() { Id = 1, Name = "Schedule 1", CronExpression = "0 0 * * * ?", IsActive = true },
            new() { Id = 2, Name = "Schedule 2", CronExpression = "0 0 12 * * ?", IsActive = false }
        };

        _mockScheduleService.Setup(x => x.GetAllSchedulesAsync())
            .ReturnsAsync(expectedSchedules);

        // Act
        var result = await _controller.GetSchedules();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSchedules = Assert.IsType<List<ScheduleDto>>(okResult.Value);
        Assert.Equal(expectedSchedules.Count, returnedSchedules.Count);
        Assert.Equal(expectedSchedules[0].Id, returnedSchedules[0].Id);
        Assert.Equal(expectedSchedules[1].Id, returnedSchedules[1].Id);
    }

    [Fact]
    public async Task GetSchedules_ReturnsEmptyList_WhenNoSchedules()
    {
        // Arrange
        _mockScheduleService.Setup(x => x.GetAllSchedulesAsync())
            .ReturnsAsync(new List<ScheduleDto>());

        // Act
        var result = await _controller.GetSchedules();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSchedules = Assert.IsType<List<ScheduleDto>>(okResult.Value);
        Assert.Empty(returnedSchedules);
    }

    #endregion

    #region GetSchedule Tests

    [Fact]
    public async Task GetSchedule_ReturnsOkResult_WhenScheduleExists()
    {
        // Arrange
        var scheduleId = 1;
        var expectedSchedule = new ScheduleDto 
        { 
            Id = scheduleId, 
            Name = "Test Schedule", 
            CronExpression = "0 0 * * * ?", 
            IsActive = true 
        };

        _mockScheduleService.Setup(x => x.GetScheduleByIdAsync(scheduleId))
            .ReturnsAsync(expectedSchedule);

        // Act
        var result = await _controller.GetSchedule(scheduleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSchedule = Assert.IsType<ScheduleDto>(okResult.Value);
        Assert.Equal(expectedSchedule.Id, returnedSchedule.Id);
        Assert.Equal(expectedSchedule.Name, returnedSchedule.Name);
        Assert.Equal(expectedSchedule.CronExpression, returnedSchedule.CronExpression);
    }

    [Fact]
    public async Task GetSchedule_ReturnsNotFound_WhenScheduleDoesNotExist()
    {
        // Arrange
        var scheduleId = 999;
        _mockScheduleService.Setup(x => x.GetScheduleByIdAsync(scheduleId))
            .ReturnsAsync((ScheduleDto?)null);

        // Act
        var result = await _controller.GetSchedule(scheduleId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetNextExecutions Tests

    [Fact]
    public void GetNextExecutions_ReturnsOkResult_WithDefaultCount()
    {
        // Arrange
        var scheduleId = 1;
        var expectedTimes = new List<DateTime>
        {
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(2),
            DateTime.UtcNow.AddHours(3),
            DateTime.UtcNow.AddHours(4),
            DateTime.UtcNow.AddHours(5)
        };

        _mockScheduleService.Setup(x => x.GetNextExecutionTimes(scheduleId, 5))
            .Returns(expectedTimes);

        // Act
        var result = _controller.GetNextExecutions(scheduleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTimes = Assert.IsType<List<DateTime>>(okResult.Value);
        Assert.Equal(expectedTimes.Count, returnedTimes.Count);
    }

    [Fact]
    public void GetNextExecutions_ReturnsOkResult_WithCustomCount()
    {
        // Arrange
        var scheduleId = 1;
        var count = 10;
        var expectedTimes = Enumerable.Range(1, count)
            .Select(i => DateTime.UtcNow.AddHours(i))
            .ToList();

        _mockScheduleService.Setup(x => x.GetNextExecutionTimes(scheduleId, count))
            .Returns(expectedTimes);

        // Act
        var result = _controller.GetNextExecutions(scheduleId, count);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTimes = Assert.IsType<List<DateTime>>(okResult.Value);
        Assert.Equal(expectedTimes.Count, returnedTimes.Count);
    }

    [Fact]
    public void GetNextExecutions_ReturnsEmptyList_WhenNoExecutions()
    {
        // Arrange
        var scheduleId = 1;
        _mockScheduleService.Setup(x => x.GetNextExecutionTimes(scheduleId, 5))
            .Returns(new List<DateTime>());

        // Act
        var result = _controller.GetNextExecutions(scheduleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTimes = Assert.IsType<List<DateTime>>(okResult.Value);
        Assert.Empty(returnedTimes);
    }

    #endregion

    #region ValidateCron Tests

    [Fact]
    public void ValidateCron_ReturnsOkResult_WithValidCronExpression()
    {
        // Arrange
        var cronExpression = "0 0 * * * ?";
        var expectedResult = CronValidationResult.Success();

        _mockScheduleService.Setup(x => x.ValidateCronExpression(cronExpression))
            .Returns(expectedResult);

        // Act
        var result = _controller.ValidateCron(cronExpression);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<CronValidationResult>(okResult.Value);
        Assert.True(returnedResult.IsValid);
        Assert.Null(returnedResult.ErrorMessage);
    }

    [Fact]
    public void ValidateCron_ReturnsOkResult_WithInvalidCronExpression()
    {
        // Arrange
        var cronExpression = "invalid cron";
        var expectedResult = CronValidationResult.Failure("Invalid cron expression");

        _mockScheduleService.Setup(x => x.ValidateCronExpression(cronExpression))
            .Returns(expectedResult);

        // Act
        var result = _controller.ValidateCron(cronExpression);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<CronValidationResult>(okResult.Value);
        Assert.False(returnedResult.IsValid);
        Assert.Equal("Invalid cron expression", returnedResult.ErrorMessage);
    }

    #endregion

    #region CreateSchedule Tests

    [Fact]
    public async Task CreateSchedule_ReturnsCreatedAtAction_WhenScheduleCreatedSuccessfully()
    {
        // Arrange
        var createDto = new CreateScheduleDto
        {
            Name = "Test Schedule",
            Description = "Test Description",
            CronExpression = "0 0 * * * ?"
        };

        var createdSchedule = new ScheduleDto
        {
            Id = 1,
            Name = "Test Schedule",
            Description = "Test Description",
            CronExpression = "0 0 * * * ?",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockScheduleService.Setup(x => x.CreateScheduleAsync(createDto))
            .ReturnsAsync(createdSchedule);

        // Act
        var result = await _controller.CreateSchedule(createDto);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(SchedulesController.GetSchedule), createdAtActionResult.ActionName);
        Assert.Equal(createdSchedule.Id, createdAtActionResult.RouteValues?["id"]);
        
        var returnedSchedule = Assert.IsType<ScheduleDto>(createdAtActionResult.Value);
        Assert.Equal(createdSchedule.Id, returnedSchedule.Id);
        Assert.Equal(createdSchedule.Name, returnedSchedule.Name);
    }

    [Fact]
    public async Task CreateSchedule_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var createDto = new CreateScheduleDto
        {
            Name = "Test Schedule",
            CronExpression = "invalid cron"
        };

        _mockScheduleService.Setup(x => x.CreateScheduleAsync(createDto))
            .ThrowsAsync(new InvalidOperationException("Invalid cron expression"));

        // Act
        var result = await _controller.CreateSchedule(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid cron expression", badRequestResult.Value?.ToString());
    }

    #endregion

    #region UpdateSchedule Tests

    [Fact]
    public async Task UpdateSchedule_ReturnsOkResult_WhenScheduleUpdatedSuccessfully()
    {
        // Arrange
        var scheduleId = 1;
        var updateDto = new UpdateScheduleDto
        {
            Description = "Updated Description",
            CronExpression = "0 0 12 * * ?",
            IsActive = false
        };

        var updatedSchedule = new ScheduleDto
        {
            Id = scheduleId,
            Name = "Test Schedule",
            Description = "Updated Description",
            CronExpression = "0 0 12 * * ?",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _mockScheduleService.Setup(x => x.UpdateScheduleAsync(scheduleId, updateDto))
            .ReturnsAsync(updatedSchedule);

        // Act
        var result = await _controller.UpdateSchedule(scheduleId, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSchedule = Assert.IsType<ScheduleDto>(okResult.Value);
        Assert.Equal(updatedSchedule.Id, returnedSchedule.Id);
        Assert.Equal(updatedSchedule.Description, returnedSchedule.Description);
        Assert.Equal(updatedSchedule.IsActive, returnedSchedule.IsActive);
    }

    [Fact]
    public async Task UpdateSchedule_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var scheduleId = 1;
        var updateDto = new UpdateScheduleDto
        {
            CronExpression = "invalid cron"
        };

        _mockScheduleService.Setup(x => x.UpdateScheduleAsync(scheduleId, updateDto))
            .ThrowsAsync(new InvalidOperationException("Invalid cron expression"));

        // Act
        var result = await _controller.UpdateSchedule(scheduleId, updateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid cron expression", badRequestResult.Value?.ToString());
    }

    #endregion

    #region DeleteSchedule Tests

    [Fact]
    public async Task DeleteSchedule_ReturnsNoContent_WhenScheduleDeletedSuccessfully()
    {
        // Arrange
        var scheduleId = 1;
        _mockScheduleService.Setup(x => x.DeleteScheduleAsync(scheduleId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteSchedule(scheduleId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteSchedule_ReturnsNotFound_WhenScheduleDoesNotExist()
    {
        // Arrange
        var scheduleId = 999;
        _mockScheduleService.Setup(x => x.DeleteScheduleAsync(scheduleId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteSchedule(scheduleId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region PauseSchedule Tests

    [Fact]
    public async Task PauseSchedule_ReturnsNoContent_WhenSchedulePausedSuccessfully()
    {
        // Arrange
        var scheduleId = 1;
        _mockScheduleService.Setup(x => x.PauseScheduleAsync(scheduleId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.PauseSchedule(scheduleId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task PauseSchedule_ReturnsBadRequest_WhenScheduleNotFound()
    {
        // Arrange
        var scheduleId = 999;
        _mockScheduleService.Setup(x => x.PauseScheduleAsync(scheduleId))
            .ThrowsAsync(new InvalidOperationException("Schedule not found"));

        // Act
        var result = await _controller.PauseSchedule(scheduleId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Schedule not found", badRequestResult.Value?.ToString());
    }

    #endregion

    #region ResumeSchedule Tests

    [Fact]
    public async Task ResumeSchedule_ReturnsNoContent_WhenScheduleResumedSuccessfully()
    {
        // Arrange
        var scheduleId = 1;
        _mockScheduleService.Setup(x => x.ResumeScheduleAsync(scheduleId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ResumeSchedule(scheduleId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ResumeSchedule_ReturnsBadRequest_WhenScheduleNotFound()
    {
        // Arrange
        var scheduleId = 999;
        _mockScheduleService.Setup(x => x.ResumeScheduleAsync(scheduleId))
            .ThrowsAsync(new InvalidOperationException("Schedule not found"));

        // Act
        var result = await _controller.ResumeSchedule(scheduleId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Schedule not found", badRequestResult.Value?.ToString());
    }

    #endregion

} 