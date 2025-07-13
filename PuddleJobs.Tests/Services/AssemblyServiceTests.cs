using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.DTOs;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;
using PuddleJobs.Tests.TestHelpers;
using System.IO;

namespace PuddleJobs.Tests.Services;

public class AssemblyServiceTests
{
    private readonly Mock<IAssemblyStorageService> _mockFileStorageService;
    private readonly Mock<ILogger<AssemblyService>> _mockLogger;
    private readonly AssemblyService _assemblyService;

    public AssemblyServiceTests()
    {
        _mockFileStorageService = new Mock<IAssemblyStorageService>();
        _mockLogger = new Mock<ILogger<AssemblyService>>();
    }

    private JobSchedulerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new JobSchedulerDbContext(options);
    }

    private AssemblyService CreateAssemblyService(JobSchedulerDbContext context)
    {
        return new AssemblyService(
            context,
            _mockFileStorageService.Object
        );
    }

    #region GetAllAssembliesAsync Tests

    [Fact]
    public async Task GetAllAssembliesAsync_ReturnsAllAssemblies_WhenAssembliesExist()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assemblies = new List<Assembly>
        {
            CreateTestAssembly(1, "Assembly 1"),
            CreateTestAssembly(2, "Assembly 2")
        };

        context.Assemblies.AddRange(assemblies);
        await context.SaveChangesAsync();

        // Act
        var result = await assemblyService.GetAllAssembliesAsync();
        var assemblyDtos = result.OrderBy(a => a.Id).ToList();

        // Assert
        Assert.NotNull(assemblyDtos);
        Assert.Equal(2, assemblyDtos.Count);
        Assert.Collection(assemblyDtos,
            assembly => Assert.Equal("Assembly 1", assembly.Name),
            assembly => Assert.Equal("Assembly 2", assembly.Name));
    }

    [Fact]
    public async Task GetAllAssembliesAsync_ReturnsEmptyList_WhenNoAssembliesExist()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);

        // Act
        var result = await assemblyService.GetAllAssembliesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetAssemblyByIdAsync Tests

    [Fact]
    public async Task GetAssemblyByIdAsync_ReturnsAssembly_WhenAssemblyExists()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();

        // Act
        var result = await assemblyService.GetAssemblyByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Assembly", result.Name);
    }

    [Fact]
    public async Task GetAssemblyByIdAsync_ReturnsNull_WhenAssemblyDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);

        // Act
        var result = await assemblyService.GetAssemblyByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAssemblyAsync Tests

    [Fact]
    public async Task CreateAssemblyAsync_CreatesAssemblySuccessfully_WhenValidDataProvided()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var createDto = TestDataBuilder.CreateCreateAssemblyDto();
        var zipData = CreateTestZipData();
        
        _mockFileStorageService.Setup(x => x.SaveAssemblyVersionAsync(
                createDto.Name, "1.0.0", zipData))
            .ReturnsAsync("/test/path");

        // Act
        var result = await assemblyService.CreateAssemblyAsync(createDto, zipData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.Name, result.Name);
        Assert.Equal(createDto.Description, result.Description);

        // Verify assembly was saved to database
        var savedAssembly = await context.Assemblies.FindAsync(result.Id);
        Assert.NotNull(savedAssembly);
        Assert.Equal(createDto.Name, savedAssembly.Name);

        // Verify version was created
        var versions = await context.AssemblyVersions
            .Where(av => av.AssemblyId == result.Id)
            .ToListAsync();
        Assert.Single(versions);
        Assert.Equal("1.0.0", versions[0].Version);
        Assert.True(versions[0].IsActive);

        _mockFileStorageService.Verify(x => x.SaveAssemblyVersionAsync(
            createDto.Name, "1.0.0", zipData), Times.Once);
    }

    [Fact]
    public async Task CreateAssemblyAsync_ThrowsException_WhenAssemblyNameAlreadyExists()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var existingAssembly = CreateTestAssembly(1, "Existing Assembly");
        context.Assemblies.Add(existingAssembly);
        await context.SaveChangesAsync();
        
        var createDto = TestDataBuilder.CreateCreateAssemblyDto();
        createDto.Name = "Existing Assembly"; // Same name as existing
        var zipData = CreateTestZipData();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => assemblyService.CreateAssemblyAsync(createDto, zipData));

        Assert.Equal("Assembly with name 'Existing Assembly' already exists.", exception.Message);
    }

    [Fact]
    public async Task CreateAssemblyAsync_ThrowsException_WhenInvalidAssemblyProvided()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var createDto = TestDataBuilder.CreateCreateAssemblyDto();
        var invalidZipData = new byte[] { 0x1, 0x2, 0x3 }; // Invalid ZIP data

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => assemblyService.CreateAssemblyAsync(createDto, invalidZipData));

        Assert.Contains("does not contain a valid assembly", exception.Message);
    }

    #endregion

    #region CreateAssemblyVersionAsync Tests

    [Fact]
    public async Task CreateAssemblyVersionAsync_CreatesVersionSuccessfully_WhenValidDataProvided()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();
        
        var createDto = TestDataBuilder.CreateCreateAssemblyVersionDto();
        var zipData = CreateTestZipData();
        
        _mockFileStorageService.Setup(x => x.SaveAssemblyVersionAsync(
                assembly.Name, createDto.Version, zipData))
            .ReturnsAsync("/test/path");

        // Act
        var result = await assemblyService.CreateAssemblyVersionAsync(assembly.Id, createDto, zipData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createDto.Version, result.Version);
        Assert.Equal(createDto.MainAssemblyName, result.FileName);
        Assert.Equal(createDto.ChangeNotes, result.ChangeNotes);
        Assert.Equal(assembly.Id, result.AssemblyId);
        Assert.True(result.IsActive); // First version should be active

        // Verify version was saved to database
        var savedVersion = await context.AssemblyVersions
            .FirstOrDefaultAsync(av => av.AssemblyId == assembly.Id && av.Version == createDto.Version);
        Assert.NotNull(savedVersion);

        _mockFileStorageService.Verify(x => x.SaveAssemblyVersionAsync(
            assembly.Name, createDto.Version, zipData), Times.Once);
    }

    [Fact]
    public async Task CreateAssemblyVersionAsync_ThrowsException_WhenAssemblyNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assemblyId = 999;
        var createDto = TestDataBuilder.CreateCreateAssemblyVersionDto();
        var zipData = CreateTestZipData();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => assemblyService.CreateAssemblyVersionAsync(assemblyId, createDto, zipData));

        Assert.Equal($"Assembly with ID {assemblyId} not found.", exception.Message);
    }

    [Fact]
    public async Task CreateAssemblyVersionAsync_ThrowsException_WhenVersionAlreadyExists()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        var existingVersion = CreateTestAssemblyVersion(1, "1.0.0", assembly.Id);
        assembly.Versions.Add(existingVersion);
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();
        
        var createDto = TestDataBuilder.CreateCreateAssemblyVersionDto();
        createDto.Version = "1.0.0"; // Same version as existing
        var zipData = CreateTestZipData();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => assemblyService.CreateAssemblyVersionAsync(assembly.Id, createDto, zipData));

        Assert.Equal("Version '1.0.0' already exists for assembly 'Test Assembly'.", exception.Message);
    }

    [Fact]
    public async Task CreateAssemblyVersionAsync_SetsAsActive_WhenFirstVersion()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();
        
        var createDto = TestDataBuilder.CreateCreateAssemblyVersionDto();
        var zipData = CreateTestZipData();
        
        _mockFileStorageService.Setup(x => x.SaveAssemblyVersionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
            .ReturnsAsync("/test/path");

        // Act
        var result = await assemblyService.CreateAssemblyVersionAsync(assembly.Id, createDto, zipData);

        // Assert
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAssemblyVersionAsync_SetsAsInactive_WhenNotFirstVersion()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        var existingVersion = CreateTestAssemblyVersion(1, "1.0.0", assembly.Id, true);
        assembly.Versions.Add(existingVersion);
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();
        
        var createDto = TestDataBuilder.CreateCreateAssemblyVersionDto();
        createDto.Version = "2.0.0";
        var zipData = CreateTestZipData();
        
        _mockFileStorageService.Setup(x => x.SaveAssemblyVersionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
            .ReturnsAsync("/test/path");

        // Act
        var result = await assemblyService.CreateAssemblyVersionAsync(assembly.Id, createDto, zipData);

        // Assert
        Assert.False(result.IsActive);
    }

    #endregion

    #region GetAssemblyVersionsAsync Tests

    [Fact]
    public async Task GetAssemblyVersionsAsync_ReturnsVersions_WhenVersionsExist()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        var versions = new List<AssemblyVersion>
        {
            CreateTestAssemblyVersion(1, "1.0.0", assembly.Id),
            CreateTestAssemblyVersion(2, "2.0.0", assembly.Id)
        };
        assembly.Versions = versions;
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();

        // Act
        var result = await assemblyService.GetAssemblyVersionsAsync(assembly.Id);
        var versionDtos = result.OrderBy(v => v.Version).ToList();

        // Assert
        Assert.NotNull(versionDtos);
        Assert.Equal(2, versionDtos.Count);
        Assert.Collection(versionDtos,
            version => Assert.Equal("1.0.0", version.Version),
            version => Assert.Equal("2.0.0", version.Version));
    }

    [Fact]
    public async Task GetAssemblyVersionsAsync_ReturnsEmptyList_WhenNoVersionsExist()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();

        // Act
        var result = await assemblyService.GetAssemblyVersionsAsync(assembly.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetAssemblyVersionAsync Tests

    [Fact]
    public async Task GetAssemblyVersionAsync_ReturnsVersion_WhenVersionExists()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        var version = CreateTestAssemblyVersion(1, "1.0.0", assembly.Id);
        assembly.Versions.Add(version);
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();

        // Act
        var result = await assemblyService.GetAssemblyVersionAsync(assembly.Id, version.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(version.Id, result.Id);
        Assert.Equal(version.Version, result.Version);
        Assert.Equal(assembly.Id, result.AssemblyId);
    }

    [Fact]
    public async Task GetAssemblyVersionAsync_ReturnsNull_WhenVersionDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();

        // Act
        var result = await assemblyService.GetAssemblyVersionAsync(assembly.Id, 999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region SetActiveVersionAsync Tests

    [Fact]
    public async Task SetActiveVersionAsync_ActivatesVersionSuccessfully_WhenVersionExists()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        var versions = new List<AssemblyVersion>
        {
            CreateTestAssemblyVersion(1, "1.0.0", assembly.Id, true), // Currently active
            CreateTestAssemblyVersion(2, "2.0.0", assembly.Id, false) // To be activated
        };
        assembly.Versions = versions;
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();

        // Act
        var result = await assemblyService.SetActiveVersionAsync(assembly.Id, versions[1].Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(versions[1].Id, result.Id);
        Assert.True(result.IsActive);

        // Verify database state
        var updatedVersions = await context.AssemblyVersions
            .Where(av => av.AssemblyId == assembly.Id)
            .ToListAsync();
        
        var version1 = updatedVersions.First(v => v.Version == "1.0.0");
        var version2 = updatedVersions.First(v => v.Version == "2.0.0");
        
        Assert.False(version1.IsActive);
        Assert.True(version2.IsActive);
    }

    [Fact]
    public async Task SetActiveVersionAsync_ThrowsException_WhenAssemblyNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assemblyId = 999;
        var versionId = 1;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => assemblyService.SetActiveVersionAsync(assemblyId, versionId));

        Assert.Equal($"Assembly with ID {assemblyId} not found.", exception.Message);
    }

    [Fact]
    public async Task SetActiveVersionAsync_ThrowsException_WhenVersionNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();
        
        var versionId = 999;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => assemblyService.SetActiveVersionAsync(assembly.Id, versionId));

        Assert.Equal($"Version with ID {versionId} not found for assembly 'Test Assembly'.", exception.Message);
    }

    #endregion

    #region DeleteAssemblyAsync Tests

    [Fact]
    public async Task DeleteAssemblyAsync_DeletesAssemblySuccessfully_WhenNoActiveJobs()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();

        // Act
        var result = await assemblyService.DeleteAssemblyAsync(assembly.Id);

        // Assert
        Assert.True(result);
        
        // Verify the assembly was soft deleted
        var deletedAssembly = await context.Assemblies.FindAsync(assembly.Id);
        Assert.True(deletedAssembly!.IsDeleted);
        Assert.NotNull(deletedAssembly.DeletedAt);
    }

    [Fact]
    public async Task DeleteAssemblyAsync_ReturnsFalse_WhenAssemblyDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assemblyId = 999;

        // Act
        var result = await assemblyService.DeleteAssemblyAsync(assemblyId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAssemblyAsync_ThrowsException_WhenAssemblyHasActiveJobs()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        var job = CreateTestJob(1, "Test Job", assembly.Id, true); // Active job
        assembly.Jobs.Add(job);
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => assemblyService.DeleteAssemblyAsync(assembly.Id));

        Assert.Equal("Cannot delete assembly 'Test Assembly' because it has active jobs.", exception.Message);
    }

    [Fact]
    public async Task DeleteAssemblyAsync_DeletesSuccessfully_WhenAssemblyHasInactiveJobs()
    {
        // Arrange
        using var context = CreateContext();
        var assemblyService = CreateAssemblyService(context);
        
        var assembly = CreateTestAssembly(1, "Test Assembly");
        var job = CreateTestJob(1, "Test Job", assembly.Id, false); // Inactive job
        assembly.Jobs.Add(job);
        context.Assemblies.Add(assembly);
        await context.SaveChangesAsync();

        // Act
        var result = await assemblyService.DeleteAssemblyAsync(assembly.Id);

        // Assert
        Assert.True(result);
        
        // Verify the assembly was soft deleted
        var deletedAssembly = await context.Assemblies.FindAsync(assembly.Id);
        Assert.True(deletedAssembly!.IsDeleted);
    }

    #endregion

    #region Helper Methods

    private static Assembly CreateTestAssembly(int id, string name, bool isDeleted = false)
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

    private static AssemblyVersion CreateTestAssemblyVersion(int id, string version, int assemblyId, bool isActive = false)
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

    private static Job CreateTestJob(int id, string name, int assemblyId, bool isActive = true)
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

    private static byte[] CreateTestZipData()
    {
        var baseDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent?.Parent?.Parent 
            ?? throw new Exception("Base directory not found.");

        var zipPath = Path.Combine(baseDirectory.FullName, "TestingApp.zip");
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException($"TestingApp.zip file not found at {zipPath}. Please ensure the file exists for testing.");
        }
        
        return File.ReadAllBytes(zipPath);
    }

    #endregion
} 