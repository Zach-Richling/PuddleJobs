using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Controllers;
using PuddleJobs.Core.DTOs;
using PuddleJobs.ApiService.Services;
using PuddleJobs.Tests.TestHelpers;
using Xunit;

namespace PuddleJobs.Tests.Controllers;

public class AssembliesControllerTests : ControllerTestBase
{
    private readonly Mock<IAssemblyService> _mockAssemblyService;
    private readonly AssembliesController _controller;

    public AssembliesControllerTests()
    {
        _mockAssemblyService = new Mock<IAssemblyService>();
        _controller = new AssembliesController(_mockAssemblyService.Object);
    }

    #region GetAssemblies Tests

    [Fact]
    public async Task GetAssemblies_ReturnsOkResult_WithAssemblies()
    {
        // Arrange
        var expectedAssemblies = new List<AssemblyDto>
        {
            new() { Id = 1, Name = "Assembly 1", Description = "Description 1" },
            new() { Id = 2, Name = "Assembly 2", Description = "Description 2" }
        };

        _mockAssemblyService.Setup(x => x.GetAllAssembliesAsync())
            .ReturnsAsync(expectedAssemblies);

        // Act
        var result = await _controller.GetAssemblies();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssemblies = Assert.IsType<List<AssemblyDto>>(okResult.Value);
        Assert.Equal(expectedAssemblies.Count, returnedAssemblies.Count);
    }

    [Fact]
    public async Task GetAssemblies_ReturnsEmptyList_WhenNoAssemblies()
    {
        // Arrange
        _mockAssemblyService.Setup(x => x.GetAllAssembliesAsync())
            .ReturnsAsync(new List<AssemblyDto>());

        // Act
        var result = await _controller.GetAssemblies();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssemblies = Assert.IsType<List<AssemblyDto>>(okResult.Value);
        Assert.Empty(returnedAssemblies);
    }

    #endregion

    #region GetAssembly Tests

    [Fact]
    public async Task GetAssembly_ReturnsOkResult_WhenAssemblyExists()
    {
        // Arrange
        var assemblyId = 1;
        var expectedAssembly = new AssemblyDto { Id = assemblyId, Name = "Test Assembly", Description = "Test Description" };

        _mockAssemblyService.Setup(x => x.GetAssemblyByIdAsync(assemblyId))
            .ReturnsAsync(expectedAssembly);

        // Act
        var result = await _controller.GetAssembly(assemblyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssembly = Assert.IsType<AssemblyDto>(okResult.Value);
        Assert.Equal(expectedAssembly.Id, returnedAssembly.Id);
        Assert.Equal(expectedAssembly.Name, returnedAssembly.Name);
    }

    [Fact]
    public async Task GetAssembly_ReturnsNotFound_WhenAssemblyDoesNotExist()
    {
        // Arrange
        var assemblyId = 999;
        _mockAssemblyService.Setup(x => x.GetAssemblyByIdAsync(assemblyId))
            .ReturnsAsync((AssemblyDto?)null);

        // Act
        var result = await _controller.GetAssembly(assemblyId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region GetAssemblyParameters Tests

    [Fact]
    public async Task GetAssemblyParameters_ReturnsOkResult_WhenParametersExist()
    {
        // Arrange
        var assemblyId = 1;
        var expectedParameters = new[]
        {
            new AssemblyParameterDefintionDto { Name = "Param1", Type = "System.String", Required = true },
            new AssemblyParameterDefintionDto { Name = "Param2", Type = "System.Int32", Required = false }
        };

        _mockAssemblyService.Setup(x => x.GetAssemblyParametersAsync(assemblyId))
            .ReturnsAsync(expectedParameters);

        // Act
        var result = await _controller.GetAssemblyParameters(assemblyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedParameters = Assert.IsType<AssemblyParameterDefintionDto[]>(okResult.Value);
        Assert.Equal(expectedParameters.Length, returnedParameters.Length);
    }

    [Fact]
    public async Task GetAssemblyParameters_ReturnsBadRequest_WhenAssemblyNotFound()
    {
        // Arrange
        var assemblyId = 999;
        _mockAssemblyService.Setup(x => x.GetAssemblyParametersAsync(assemblyId))
            .ThrowsAsync(new InvalidOperationException("Assembly not found"));

        // Act
        var result = await _controller.GetAssemblyParameters(assemblyId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Assembly not found", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task GetAssemblyParameters_ReturnsBadRequest_WhenExceptionOccurs()
    {
        // Arrange
        var assemblyId = 1;
        _mockAssemblyService.Setup(x => x.GetAssemblyParametersAsync(assemblyId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAssemblyParameters(assemblyId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Error loading assembly parameters", badRequestResult.Value?.ToString());
    }

    #endregion

    #region GetVersions Tests

    [Fact]
    public async Task GetVersions_ReturnsOkResult_WithVersions()
    {
        // Arrange
        var assemblyId = 1;
        var expectedVersions = new List<AssemblyVersionDto>
        {
            new() { Id = 1, Version = "1.0.0", AssemblyId = assemblyId, IsActive = true },
            new() { Id = 2, Version = "1.1.0", AssemblyId = assemblyId, IsActive = false }
        };

        _mockAssemblyService.Setup(x => x.GetAssemblyVersionsAsync(assemblyId))
            .ReturnsAsync(expectedVersions);

        // Act
        var result = await _controller.GetVersions(assemblyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedVersions = Assert.IsType<List<AssemblyVersionDto>>(okResult.Value);
        Assert.Equal(expectedVersions.Count, returnedVersions.Count);
    }

    #endregion

    #region GetVersion Tests

    [Fact]
    public async Task GetVersion_ReturnsOkResult_WhenVersionExists()
    {
        // Arrange
        var assemblyId = 1;
        var versionId = 1;
        var expectedVersion = new AssemblyVersionDto { Id = versionId, Version = "1.0.0", AssemblyId = assemblyId, IsActive = true };

        _mockAssemblyService.Setup(x => x.GetAssemblyVersionAsync(assemblyId, versionId))
            .ReturnsAsync(expectedVersion);

        // Act
        var result = await _controller.GetVersion(assemblyId, versionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedVersion = Assert.IsType<AssemblyVersionDto>(okResult.Value);
        Assert.Equal(expectedVersion.Id, returnedVersion.Id);
        Assert.Equal(expectedVersion.Version, returnedVersion.Version);
    }

    [Fact]
    public async Task GetVersion_ReturnsNotFound_WhenVersionDoesNotExist()
    {
        // Arrange
        var assemblyId = 1;
        var versionId = 999;
        _mockAssemblyService.Setup(x => x.GetAssemblyVersionAsync(assemblyId, versionId))
            .ReturnsAsync((AssemblyVersionDto?)null);

        // Act
        var result = await _controller.GetVersion(assemblyId, versionId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region SetActiveVersion Tests

    [Fact]
    public async Task SetActiveVersion_ReturnsOkResult_WhenVersionActivated()
    {
        // Arrange
        var assemblyId = 1;
        var versionId = 2;
        var expectedVersion = new AssemblyVersionDto { Id = versionId, Version = "1.1.0", AssemblyId = assemblyId, IsActive = true };

        _mockAssemblyService.Setup(x => x.SetActiveVersionAsync(assemblyId, versionId))
            .ReturnsAsync(expectedVersion);

        // Act
        var result = await _controller.SetActiveVersion(assemblyId, versionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedVersion = Assert.IsType<AssemblyVersionDto>(okResult.Value);
        Assert.Equal(expectedVersion.Id, returnedVersion.Id);
        Assert.True(returnedVersion.IsActive);
    }

    [Fact]
    public async Task SetActiveVersion_ReturnsBadRequest_WhenExceptionOccurs()
    {
        // Arrange
        var assemblyId = 1;
        var versionId = 999;
        _mockAssemblyService.Setup(x => x.SetActiveVersionAsync(assemblyId, versionId))
            .ThrowsAsync(new InvalidOperationException("Version not found"));

        // Act
        var result = await _controller.SetActiveVersion(assemblyId, versionId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Version not found", badRequestResult.Value?.ToString());
    }

    #endregion

    #region DeleteAssembly Tests

    [Fact]
    public async Task DeleteAssembly_ReturnsNoContent_WhenAssemblyDeleted()
    {
        // Arrange
        var assemblyId = 1;
        _mockAssemblyService.Setup(x => x.DeleteAssemblyAsync(assemblyId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteAssembly(assemblyId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteAssembly_ReturnsNotFound_WhenAssemblyDoesNotExist()
    {
        // Arrange
        var assemblyId = 999;
        _mockAssemblyService.Setup(x => x.DeleteAssemblyAsync(assemblyId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteAssembly(assemblyId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAssembly_ReturnsBadRequest_WhenExceptionOccurs()
    {
        // Arrange
        var assemblyId = 1;
        _mockAssemblyService.Setup(x => x.DeleteAssemblyAsync(assemblyId))
            .ThrowsAsync(new InvalidOperationException("Cannot delete assembly with active jobs"));

        // Act
        var result = await _controller.DeleteAssembly(assemblyId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Cannot delete assembly with active jobs", badRequestResult.Value?.ToString());
    }

    #endregion
} 