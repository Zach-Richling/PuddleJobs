using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Controllers;
using PuddleJobs.ApiService.DTOs;
using PuddleJobs.ApiService.Helpers;
using PuddleJobs.ApiService.Services;
using PuddleJobs.Tests.TestHelpers;
using Xunit;

namespace PuddleJobs.Tests.Controllers;

public class JobsControllerTests : ControllerTestBase
{
    private readonly Mock<IJobService> _mockJobService;
    private readonly Mock<IJobParameterService> _mockJobParameterService;
    private readonly Mock<ILogger<JobsController>> _mockLogger;
    private readonly JobsController _controller;

    public JobsControllerTests()
    {
        _mockJobService = new Mock<IJobService>();
        _mockJobParameterService = new Mock<IJobParameterService>();
        _mockLogger = new Mock<ILogger<JobsController>>();
        _controller = new JobsController(_mockJobService.Object, _mockJobParameterService.Object);
    }

    #region GetJobs Tests

    [Fact]
    public async Task GetJobs_ReturnsOkResult_WithJobs()
    {
        // Arrange
        var expectedJobs = new List<JobDto>
        {
            new() { Id = 1, Name = "Job 1", IsActive = true },
            new() { Id = 2, Name = "Job 2", IsActive = false }
        };

        _mockJobService.Setup(x => x.GetAllJobsAsync())
            .ReturnsAsync(expectedJobs);

        // Act
        var result = await _controller.GetJobs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedJobs = Assert.IsType<List<JobDto>>(okResult.Value);
        Assert.Equal(expectedJobs.Count, returnedJobs.Count);
        Assert.Equal(expectedJobs[0].Id, returnedJobs[0].Id);
        Assert.Equal(expectedJobs[1].Id, returnedJobs[1].Id);
    }

    [Fact]
    public async Task GetJobs_ReturnsEmptyList_WhenNoJobs()
    {
        // Arrange
        _mockJobService.Setup(x => x.GetAllJobsAsync())
            .ReturnsAsync(new List<JobDto>());

        // Act
        var result = await _controller.GetJobs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedJobs = Assert.IsType<List<JobDto>>(okResult.Value);
        Assert.Empty(returnedJobs);
    }

    #endregion

    #region GetJob Tests

    [Fact]
    public async Task GetJob_ReturnsOkResult_WhenJobExists()
    {
        // Arrange
        var jobId = 1;
        var expectedJob = new JobDto { Id = jobId, Name = "Test Job", IsActive = true };

        _mockJobService.Setup(x => x.GetJobByIdAsync(jobId))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _controller.GetJob(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedJob = Assert.IsType<JobDto>(okResult.Value);
        Assert.Equal(expectedJob.Id, returnedJob.Id);
        Assert.Equal(expectedJob.Name, returnedJob.Name);
    }

    [Fact]
    public async Task GetJob_ReturnsNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var jobId = 999;
        _mockJobService.Setup(x => x.GetJobByIdAsync(jobId))
            .ReturnsAsync((JobDto?)null);

        // Act
        var result = await _controller.GetJob(jobId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetJobParameters Tests

    [Fact]
    public async Task GetJobParameters_ReturnsOkResult_WhenParametersExist()
    {
        // Arrange
        var jobId = 1;
        var expectedParameters = new[]
        {
            new JobParameterInfo { Name = "Param1", Type = "System.String", Required = true },
            new JobParameterInfo { Name = "Param2", Type = "System.Int32", Required = false }
        };

        _mockJobService.Setup(x => x.GetJobParametersAsync(jobId))
            .ReturnsAsync(expectedParameters);

        // Act
        var result = await _controller.GetJobParameters(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedParameters = Assert.IsType<JobParameterInfo[]>(okResult.Value);
        Assert.Equal(expectedParameters.Length, returnedParameters.Length);
    }

    [Fact]
    public async Task GetJobParameters_ReturnsBadRequest_WhenJobNotFound()
    {
        // Arrange
        var jobId = 999;
        _mockJobService.Setup(x => x.GetJobParametersAsync(jobId))
            .ThrowsAsync(new InvalidOperationException("Job not found"));

        // Act
        var result = await _controller.GetJobParameters(jobId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Job not found", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task GetJobParameters_ReturnsBadRequest_WhenExceptionOccurs()
    {
        // Arrange
        var jobId = 1;
        _mockJobService.Setup(x => x.GetJobParametersAsync(jobId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetJobParameters(jobId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Error loading job parameters", badRequestResult.Value?.ToString());
    }

    #endregion

    #region GetJobParameterValues Tests

    [Fact]
    public async Task GetJobParameterValues_ReturnsOkResult_WhenParametersExist()
    {
        // Arrange
        var jobId = 1;
        var expectedParameters = new[]
        {
            new JobParameterDto { Id = 1, Name = "Param1", Value = "Value1", JobId = jobId },
            new JobParameterDto { Id = 2, Name = "Param2", Value = "Value2", JobId = jobId }
        };

        _mockJobParameterService.Setup(x => x.GetJobParameterValuesAsync(jobId))
            .ReturnsAsync(expectedParameters);

        // Act
        var result = await _controller.GetJobParameterValues(jobId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedParameters = Assert.IsAssignableFrom<IEnumerable<JobParameterDto>>(okResult.Value);
        Assert.Equal(expectedParameters.Length, returnedParameters.Count());
    }

    [Fact]
    public async Task GetJobParameterValues_ReturnsBadRequest_WhenExceptionOccurs()
    {
        // Arrange
        var jobId = 999;
        _mockJobParameterService.Setup(x => x.GetJobParameterValuesAsync(jobId))
            .ThrowsAsync(new InvalidOperationException("Job not found"));

        // Act
        var result = await _controller.GetJobParameterValues(jobId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Job not found", badRequestResult.Value?.ToString());
    }

    #endregion

    #region SetJobParameterValues Tests

    [Fact]
    public async Task SetJobParameterValues_ReturnsNoContent_WhenParametersSetSuccessfully()
    {
        // Arrange
        var jobId = 1;
        var parameters = new[]
        {
            new JobParameterValueDto { Name = "Param1", Value = "Value1" },
            new JobParameterValueDto { Name = "Param2", Value = "Value2" }
        };

        _mockJobParameterService.Setup(x => x.SetJobParameterValuesAsync(jobId, parameters))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SetJobParameterValues(jobId, parameters);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SetJobParameterValues_ReturnsBadRequest_WhenExceptionOccurs()
    {
        // Arrange
        var jobId = 999;
        var parameters = new[]
        {
            new JobParameterValueDto { Name = "Param1", Value = "Value1" }
        };

        _mockJobParameterService.Setup(x => x.SetJobParameterValuesAsync(jobId, parameters))
            .ThrowsAsync(new InvalidOperationException("Job not found"));

        // Act
        var result = await _controller.SetJobParameterValues(jobId, parameters);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Job not found", badRequestResult.Value?.ToString());
    }

    #endregion

    #region CreateJob Tests

    [Fact]
    public async Task CreateJob_ReturnsCreatedAtAction_WhenJobCreatedSuccessfully()
    {
        // Arrange
        var createDto = new CreateJobDto
        {
            Name = "Test Job",
            Description = "Test Description",
            AssemblyId = 1,
            ScheduleIds = new List<int> { 1, 2 },
            Parameters = new List<JobParameterValueDto>
            {
                new() { Name = "Param1", Value = "Value1" }
            }
        };

        var createdJob = new JobDto
        {
            Id = 1,
            Name = "Test Job",
            Description = "Test Description",
            AssemblyId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockJobService.Setup(x => x.CreateJobAsync(createDto))
            .ReturnsAsync(createdJob);

        // Act
        var result = await _controller.CreateJob(createDto);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(JobsController.GetJob), createdAtActionResult.ActionName);
        Assert.Equal(createdJob.Id, createdAtActionResult.RouteValues?["id"]);
        
        var returnedJob = Assert.IsType<JobDto>(createdAtActionResult.Value);
        Assert.Equal(createdJob.Id, returnedJob.Id);
        Assert.Equal(createdJob.Name, returnedJob.Name);
    }

    [Fact]
    public async Task CreateJob_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var createDto = new CreateJobDto
        {
            Name = "Test Job",
            AssemblyId = 1,
            ScheduleIds = new List<int>(),
            Parameters = new List<JobParameterValueDto>()
        };

        _mockJobService.Setup(x => x.CreateJobAsync(createDto))
            .ThrowsAsync(new InvalidOperationException("Invalid assembly ID"));

        // Act
        var result = await _controller.CreateJob(createDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid assembly ID", badRequestResult.Value?.ToString());
    }

    #endregion

    #region UpdateJob Tests

    [Fact]
    public async Task UpdateJob_ReturnsOkResult_WhenJobUpdatedSuccessfully()
    {
        // Arrange
        var jobId = 1;
        var updateDto = new UpdateJobDto
        {
            Name = "Updated Job",
            Description = "Updated Description",
            IsActive = false,
            ScheduleIds = new List<int> { 1, 3 },
            Parameters = new List<JobParameterValueDto>
            {
                new() { Name = "Param1", Value = "UpdatedValue1" }
            }
        };

        var updatedJob = new JobDto
        {
            Id = jobId,
            Name = "Updated Job",
            Description = "Updated Description",
            AssemblyId = 1,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _mockJobService.Setup(x => x.UpdateJobAsync(jobId, updateDto))
            .ReturnsAsync(updatedJob);

        // Act
        var result = await _controller.UpdateJob(jobId, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedJob = Assert.IsType<JobDto>(okResult.Value);
        Assert.Equal(updatedJob.Id, returnedJob.Id);
        Assert.Equal(updatedJob.Name, returnedJob.Name);
        Assert.Equal(updatedJob.IsActive, returnedJob.IsActive);
    }

    [Fact]
    public async Task UpdateJob_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var jobId = 1;
        var updateDto = new UpdateJobDto
        {
            Name = "Updated Job",
            ScheduleIds = new List<int>(),
            Parameters = new List<JobParameterValueDto>()
        };

        _mockJobService.Setup(x => x.UpdateJobAsync(jobId, updateDto))
            .ThrowsAsync(new InvalidOperationException("Job not found"));

        // Act
        var result = await _controller.UpdateJob(jobId, updateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Job not found", badRequestResult.Value?.ToString());
    }

    #endregion

    #region DeleteJob Tests

    [Fact]
    public async Task DeleteJob_ReturnsNoContent_WhenJobDeletedSuccessfully()
    {
        // Arrange
        var jobId = 1;
        _mockJobService.Setup(x => x.DeleteJobAsync(jobId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteJob(jobId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteJob_ReturnsNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var jobId = 999;
        _mockJobService.Setup(x => x.DeleteJobAsync(jobId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteJob(jobId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

} 