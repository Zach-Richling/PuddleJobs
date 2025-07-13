using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Controllers;
using PuddleJobs.ApiService.DTOs;
using PuddleJobs.ApiService.Services;
using PuddleJobs.Tests.TestHelpers;
using Xunit;

namespace PuddleJobs.Tests.Controllers;

public class AssembliesControllerTests : ControllerTestBase
{
    private readonly Mock<IAssemblyService> _mockAssemblyService;
    private readonly Mock<ILogger<AssembliesController>> _mockLogger;
    private readonly AssembliesController _controller;

    public AssembliesControllerTests()
    {
        _mockAssemblyService = new Mock<IAssemblyService>();
        _mockLogger = new Mock<ILogger<AssembliesController>>();
        _controller = new AssembliesController(_mockAssemblyService.Object);
    }

    #region GetAssemblies Tests

    [Fact]
    public async Task GetAssemblies_ReturnsOkResult_WithAssemblies()
    {
        // Arrange
        var expectedAssemblies = new List<AssemblyDto>
        {
            new() { Id = 1, Name = "Assembly 1", Description = "Test Assembly 1" },
            new() { Id = 2, Name = "Assembly 2", Description = "Test Assembly 2" }
        };

        _mockAssemblyService.Setup(x => x.GetAllAssembliesAsync())
            .ReturnsAsync(expectedAssemblies);

        // Act
        var result = await _controller.GetAssemblies();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssemblies = Assert.IsType<List<AssemblyDto>>(okResult.Value);
        Assert.Equal(expectedAssemblies.Count, returnedAssemblies.Count);
        Assert.Equal(expectedAssemblies[0].Id, returnedAssemblies[0].Id);
        Assert.Equal(expectedAssemblies[1].Id, returnedAssemblies[1].Id);
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
        var expectedAssembly = new AssemblyDto 
        { 
            Id = assemblyId, 
            Name = "Test Assembly", 
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow
        };

        _mockAssemblyService.Setup(x => x.GetAssemblyByIdAsync(assemblyId))
            .ReturnsAsync(expectedAssembly);

        // Act
        var result = await _controller.GetAssembly(assemblyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAssembly = Assert.IsType<AssemblyDto>(okResult.Value);
        Assert.Equal(expectedAssembly.Id, returnedAssembly.Id);
        Assert.Equal(expectedAssembly.Name, returnedAssembly.Name);
        Assert.Equal(expectedAssembly.Description, returnedAssembly.Description);
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

    #region CreateAssembly Tests

    [Fact]
    public async Task CreateAssembly_ReturnsCreatedAtAction_WhenAssemblyCreatedSuccessfully()
    {
        // Arrange
        var createDto = new CreateAssemblyDto
        {
            Name = "Test Assembly",
            Description = "Test Description",
            MainAssemblyName = "TestAssembly.dll"
        };

        var zipFile = CreateMockZipFile("test.zip", new byte[] { 1, 2, 3, 4 });

        var createdAssembly = new AssemblyDto
        {
            Id = 1,
            Name = "Test Assembly",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow
        };

        _mockAssemblyService.Setup(x => x.CreateAssemblyAsync(createDto, It.IsAny<byte[]>()))
            .ReturnsAsync(createdAssembly);

        // Act
        var result = await _controller.CreateAssembly(createDto, zipFile);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(AssembliesController.GetAssembly), createdAtActionResult.ActionName);
        Assert.Equal(createdAssembly.Id, createdAtActionResult.RouteValues?["id"]);
        
        var returnedAssembly = Assert.IsType<AssemblyDto>(createdAtActionResult.Value);
        Assert.Equal(createdAssembly.Id, returnedAssembly.Id);
        Assert.Equal(createdAssembly.Name, returnedAssembly.Name);
    }

    [Fact]
    public async Task CreateAssembly_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var createDto = new CreateAssemblyDto
        {
            Name = "Test Assembly",
            MainAssemblyName = "TestAssembly.dll"
        };

        var zipFile = CreateMockZipFile("test.zip", new byte[] { 1, 2, 3, 4 });

        _mockAssemblyService.Setup(x => x.CreateAssemblyAsync(createDto, It.IsAny<byte[]>()))
            .ThrowsAsync(new InvalidOperationException("Assembly name already exists"));

        // Act
        var result = await _controller.CreateAssembly(createDto, zipFile);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Assembly name already exists", badRequestResult.Value?.ToString());
    }

    #endregion

    #region UploadVersion Tests

    [Fact]
    public async Task UploadVersion_ReturnsCreatedAtAction_WhenVersionUploadedSuccessfully()
    {
        // Arrange
        var assemblyId = 1;
        var createDto = new CreateAssemblyVersionDto
        {
            Version = "1.0.0",
            MainAssemblyName = "TestAssembly.dll",
            ChangeNotes = "Initial version"
        };

        var zipFile = CreateMockZipFile("test.zip", [1, 2, 3, 4]);

        var createdVersion = new AssemblyVersionDto
        {
            Id = 1,
            Version = "1.0.0",
            FileName = "test.zip",
            UploadedAt = DateTime.UtcNow,
            ChangeNotes = "Initial version",
            AssemblyId = assemblyId,
            IsActive = false
        };

        _mockAssemblyService.Setup(x => x.CreateAssemblyVersionAsync(assemblyId, createDto, It.IsAny<byte[]>()))
            .ReturnsAsync(createdVersion);

        // Act
        var result = await _controller.UploadVersion(assemblyId, createDto, zipFile);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(AssembliesController.GetVersion), createdAtActionResult.ActionName);
        Assert.Equal(assemblyId, createdAtActionResult.RouteValues?["id"]);
        Assert.Equal(createdVersion.Id, createdAtActionResult.RouteValues?["versionId"]);
        
        var returnedVersion = Assert.IsType<AssemblyVersionDto>(createdAtActionResult.Value);
        Assert.Equal(createdVersion.Id, returnedVersion.Id);
        Assert.Equal(createdVersion.Version, returnedVersion.Version);
    }

    [Fact]
    public async Task UploadVersion_ReturnsBadRequest_WhenNoZipFile()
    {
        // Arrange
        var assemblyId = 1;
        var createDto = new CreateAssemblyVersionDto
        {
            Version = "1.0.0",
            MainAssemblyName = "TestAssembly.dll"
        };

        IFormFile? zipFile = null!;

        // Act
        var result = await _controller.UploadVersion(assemblyId, createDto, zipFile);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("No ZIP file uploaded", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task UploadVersion_ReturnsBadRequest_WhenEmptyZipFile()
    {
        // Arrange
        var assemblyId = 1;
        var createDto = new CreateAssemblyVersionDto
        {
            Version = "1.0.0",
            MainAssemblyName = "TestAssembly.dll"
        };

        var zipFile = CreateMockZipFile("test.zip", []);

        // Act
        var result = await _controller.UploadVersion(assemblyId, createDto, zipFile);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("No ZIP file uploaded", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task UploadVersion_ReturnsBadRequest_WhenMainAssemblyNameIsEmpty()
    {
        // Arrange
        var assemblyId = 1;
        var createDto = new CreateAssemblyVersionDto
        {
            Version = "1.0.0",
            MainAssemblyName = ""
        };

        var zipFile = CreateMockZipFile("test.zip", new byte[] { 1, 2, 3, 4 });

        // Act
        var result = await _controller.UploadVersion(assemblyId, createDto, zipFile);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Main assembly name is required", badRequestResult.Value?.ToString());
    }

    [Fact]
    public async Task UploadVersion_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var assemblyId = 1;
        var createDto = new CreateAssemblyVersionDto
        {
            Version = "1.0.0",
            MainAssemblyName = "TestAssembly.dll"
        };

        var zipFile = CreateMockZipFile("test.zip", new byte[] { 1, 2, 3, 4 });

        _mockAssemblyService.Setup(x => x.CreateAssemblyVersionAsync(assemblyId, createDto, It.IsAny<byte[]>()))
            .ThrowsAsync(new InvalidOperationException("Invalid ZIP file format"));

        // Act
        var result = await _controller.UploadVersion(assemblyId, createDto, zipFile);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid ZIP file format", badRequestResult.Value?.ToString());
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
        Assert.Equal(expectedVersions[0].Id, returnedVersions[0].Id);
        Assert.Equal(expectedVersions[1].Id, returnedVersions[1].Id);
    }

    [Fact]
    public async Task GetVersions_ReturnsEmptyList_WhenNoVersions()
    {
        // Arrange
        var assemblyId = 1;
        _mockAssemblyService.Setup(x => x.GetAssemblyVersionsAsync(assemblyId))
            .ReturnsAsync(new List<AssemblyVersionDto>());

        // Act
        var result = await _controller.GetVersions(assemblyId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedVersions = Assert.IsType<List<AssemblyVersionDto>>(okResult.Value);
        Assert.Empty(returnedVersions);
    }

    #endregion

    #region GetVersion Tests

    [Fact]
    public async Task GetVersion_ReturnsOkResult_WhenVersionExists()
    {
        // Arrange
        var assemblyId = 1;
        var versionId = 1;
        var expectedVersion = new AssemblyVersionDto 
        { 
            Id = versionId, 
            Version = "1.0.0", 
            AssemblyId = assemblyId,
            IsActive = true,
            UploadedAt = DateTime.UtcNow
        };

        _mockAssemblyService.Setup(x => x.GetAssemblyVersionAsync(assemblyId, versionId))
            .ReturnsAsync(expectedVersion);

        // Act
        var result = await _controller.GetVersion(assemblyId, versionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedVersion = Assert.IsType<AssemblyVersionDto>(okResult.Value);
        Assert.Equal(expectedVersion.Id, returnedVersion.Id);
        Assert.Equal(expectedVersion.Version, returnedVersion.Version);
        Assert.Equal(expectedVersion.AssemblyId, returnedVersion.AssemblyId);
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
    public async Task SetActiveVersion_ReturnsOkResult_WhenVersionActivatedSuccessfully()
    {
        // Arrange
        var assemblyId = 1;
        var versionId = 2;
        var activatedVersion = new AssemblyVersionDto
        {
            Id = versionId,
            Version = "1.1.0",
            AssemblyId = assemblyId,
            IsActive = true,
            UploadedAt = DateTime.UtcNow
        };

        _mockAssemblyService.Setup(x => x.SetActiveVersionAsync(assemblyId, versionId))
            .ReturnsAsync(activatedVersion);

        // Act
        var result = await _controller.SetActiveVersion(assemblyId, versionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedVersion = Assert.IsType<AssemblyVersionDto>(okResult.Value);
        Assert.Equal(activatedVersion.Id, returnedVersion.Id);
        Assert.True(returnedVersion.IsActive);
    }

    [Fact]
    public async Task SetActiveVersion_ReturnsBadRequest_WhenValidationFails()
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
    public async Task DeleteAssembly_ReturnsNoContent_WhenAssemblyDeletedSuccessfully()
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
    public async Task DeleteAssembly_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var assemblyId = 1;
        _mockAssemblyService.Setup(x => x.DeleteAssemblyAsync(assemblyId))
            .ThrowsAsync(new InvalidOperationException("Assembly has active jobs"));

        // Act
        var result = await _controller.DeleteAssembly(assemblyId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Assembly has active jobs", badRequestResult.Value?.ToString());
    }

    #endregion

    #region Helper Methods

    private static IFormFile CreateMockZipFile(string fileName, byte[] content)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mockFile.Object;
    }

    #endregion
} 