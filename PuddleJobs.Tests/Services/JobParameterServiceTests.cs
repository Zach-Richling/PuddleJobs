using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Data;
using PuddleJobs.Core.DTOs;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;
using PuddleJobs.Tests.TestHelpers;

namespace PuddleJobs.Tests.Services;

public class JobParameterServiceTests
{
    private readonly Mock<ILogger<JobParameterService>> _mockLogger;

    public JobParameterServiceTests()
    {
        _mockLogger = new Mock<ILogger<JobParameterService>>();
    }

    private JobSchedulerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<JobSchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new JobSchedulerDbContext(options);
    }

    private JobParameterService CreateJobParameterService(JobSchedulerDbContext context)
    {
        return new JobParameterService(context);
    }

    #region GetJobParameterValuesAsync Tests

    [Fact]
    public async Task GetJobParameterValuesAsync_ReturnsParameters_WhenParametersExist()
    {
        // Arrange
        using var context = CreateContext();
        var jobParameterService = CreateJobParameterService(context);
        
        var jobId = 1;
        var job = CreateTestJob(jobId, "Test Job");
        var parameters = new List<JobParameter>
        {
            CreateTestJobParameter(1, "Param1", "Value1", jobId),
            CreateTestJobParameter(2, "Param2", "Value2", jobId)
        };
        
        context.Jobs.Add(job);
        context.JobParameters.AddRange(parameters);
        await context.SaveChangesAsync();

        // Act
        var result = await jobParameterService.GetJobParameterValuesAsync(jobId);
        var parameterDtos = result.OrderBy(p => p.Id).ToList();

        // Assert
        Assert.NotNull(parameterDtos);
        Assert.Equal(2, parameterDtos.Count);
        Assert.Collection(parameterDtos,
            param => Assert.Equal("Param1", param.Name),
            param => Assert.Equal("Param2", param.Name));
    }

    [Fact]
    public async Task GetJobParameterValuesAsync_ReturnsEmptyList_WhenNoParametersExist()
    {
        // Arrange
        using var context = CreateContext();
        var jobParameterService = CreateJobParameterService(context);
        
        var jobId = 1;
        var job = CreateTestJob(jobId, "Test Job");
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        var result = await jobParameterService.GetJobParameterValuesAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetJobParameterValuesAsync_ThrowsException_WhenJobDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var jobParameterService = CreateJobParameterService(context);
        
        var jobId = 999;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => jobParameterService.GetJobParameterValuesAsync(jobId));

        Assert.Equal($"Job with ID {jobId} not found.", exception.Message);
    }

    #endregion

    #region SetJobParameterValuesAsync Tests

    [Fact]
    public async Task SetJobParameterValuesAsync_SetsParametersSuccessfully_WhenValidDataProvided()
    {
        // Arrange
        using var context = CreateContext();
        var jobParameterService = CreateJobParameterService(context);
        
        var assemblyId = 1;
        var assembly = CreateTestAssembly(assemblyId, "Test Assembly");
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", assemblyId, true);
        var parameterDefinitions = new List<AssemblyParameterDefinition>
        {
            CreateTestParameterDefinition(1, "Param1", "System.String", false, assemblyVersion.Id),
            CreateTestParameterDefinition(2, "Param2", "System.String", false, assemblyVersion.Id),
            CreateTestParameterDefinition(3, "ExistingParam", "System.String", false, assemblyVersion.Id)
        };
        
        assemblyVersion.ParameterDefinitions = parameterDefinitions;
        assembly.Versions.Add(assemblyVersion);
        
        var jobId = 1;
        var job = CreateTestJob(jobId, "Test Job", assemblyId);
        var existingParameter = CreateTestJobParameter(1, "ExistingParam", "OldValue", jobId);
        
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.AssemblyParameterDefinitions.AddRange(parameterDefinitions);
        context.Jobs.Add(job);
        context.JobParameters.Add(existingParameter);

        await context.SaveChangesAsync();
        
        var parameters = TestDataBuilder.CreateJobParameterValues();

        // Act
        var result = await jobParameterService.SetJobParameterValuesAsync(jobId, parameters);

        // Assert
        Assert.True(result);

        // Verify parameters were set correctly
        var jobParameters = await context.JobParameters
            .Where(p => p.JobId == jobId)
            .ToListAsync();
        
        Assert.Equal(3, jobParameters.Count);
        
        var param1 = jobParameters.FirstOrDefault(p => p.Name == "Param1");
        var param2 = jobParameters.FirstOrDefault(p => p.Name == "Param2");
        var existingParam = jobParameters.FirstOrDefault(p => p.Name == "ExistingParam");
        
        Assert.NotNull(param1);
        Assert.NotNull(param2);
        Assert.NotNull(existingParam);
        Assert.Equal("Value1", param1.Value);
        Assert.Equal("Value2", param2.Value);
        Assert.Null(existingParam.Value);
        Assert.NotNull(existingParam.UpdatedAt);
    }

    [Fact]
    public async Task SetJobParameterValuesAsync_ThrowsException_WhenJobNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var jobParameterService = CreateJobParameterService(context);
        
        var jobId = 999;
        var parameters = TestDataBuilder.CreateJobParameterValues();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => jobParameterService.SetJobParameterValuesAsync(jobId, parameters));

        Assert.Equal($"Job with ID {jobId} not found.", exception.Message);
    }

    [Fact]
    public async Task SetJobParameterValuesAsync_OverwritesExistingValueWithNull_WhenParameterExists()
    {
        // Arrange
        using var context = CreateContext();
        var jobParameterService = CreateJobParameterService(context);
        
        var assemblyId = 1;
        var assembly = CreateTestAssembly(assemblyId, "Test Assembly");
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", assemblyId, true);
        var parameterDefinitions = new List<AssemblyParameterDefinition>
        {
            CreateTestParameterDefinition(1, "ExistingParam", "System.String", false, assemblyVersion.Id),
            CreateTestParameterDefinition(2, "NewParam", "System.String", false, assemblyVersion.Id)
        };
        
        assemblyVersion.ParameterDefinitions = parameterDefinitions;
        assembly.Versions.Add(assemblyVersion);
        
        var jobId = 1;
        var job = CreateTestJob(jobId, "Test Job", assemblyId);
        var existingParameter = CreateTestJobParameter(1, "ExistingParam", "OriginalValue", jobId);
        
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.AssemblyParameterDefinitions.AddRange(parameterDefinitions);
        context.Jobs.Add(job);
        context.JobParameters.Add(existingParameter);
        await context.SaveChangesAsync();
        
        var parameters = new List<JobParameterValueDto>
        {
            new() { Name = "ExistingParam", Value = null },
            new() { Name = "NewParam", Value = "NewValue" }
        };

        // Act
        var result = await jobParameterService.SetJobParameterValuesAsync(jobId, parameters);

        // Assert
        Assert.True(result);

        // Verify parameters were set correctly
        var jobParameters = await context.JobParameters
            .Where(p => p.JobId == jobId)
            .ToListAsync();
        
        Assert.Equal(2, jobParameters.Count);
        
        var overwrittenParam = jobParameters.FirstOrDefault(p => p.Name == "ExistingParam");
        var newParam = jobParameters.FirstOrDefault(p => p.Name == "NewParam");
        
        Assert.NotNull(overwrittenParam);
        Assert.NotNull(newParam);
        Assert.Null(overwrittenParam.Value);
        Assert.Equal("NewValue", newParam.Value);
        Assert.NotNull(overwrittenParam.UpdatedAt);
    }

    #endregion

    #region SetJobParameterValuesAsync Validation Tests

    [Fact]
    public async Task SetJobParameterValuesAsync_ThrowsException_WhenRequiredParameterMissing()
    {
        // Arrange
        using var context = CreateContext();
        var jobParameterService = CreateJobParameterService(context);
        
        var assemblyId = 1;
        var assembly = CreateTestAssembly(assemblyId, "Test Assembly");
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", assemblyId, true);
        var parameterDefinitions = new List<AssemblyParameterDefinition>
        {
            CreateTestParameterDefinition(1, "RequiredParam", "System.String", true, assemblyVersion.Id)
        };
        
        assemblyVersion.ParameterDefinitions = parameterDefinitions;
        assembly.Versions.Add(assemblyVersion);
        
        var job = CreateTestJob(1, "Test Job", assemblyId);
        
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.AssemblyParameterDefinitions.AddRange(parameterDefinitions);
        context.Jobs.Add(job);
        await context.SaveChangesAsync();
        
        var parameters = new List<JobParameterValueDto>(); // No parameters provided

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => jobParameterService.SetJobParameterValuesAsync(job.Id, parameters));

        Assert.Equal("Required parameter 'RequiredParam' is missing.", exception.Message);
    }

    [Fact]
    public async Task SetJobParameterValuesAsync_ThrowsException_WhenParameterTypeInvalid()
    {
        // Arrange
        using var context = CreateContext();
        var jobParameterService = CreateJobParameterService(context);
        
        var assemblyId = 1;
        var assembly = CreateTestAssembly(assemblyId, "Test Assembly");
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", assemblyId, true);
        var parameterDefinitions = new List<AssemblyParameterDefinition>
        {
            CreateTestParameterDefinition(1, "IntParam", "System.Int32", true, assemblyVersion.Id)
        };
        
        assemblyVersion.ParameterDefinitions = parameterDefinitions;
        assembly.Versions.Add(assemblyVersion);
        
        var job = CreateTestJob(1, "Test Job", assemblyId);
        
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.AssemblyParameterDefinitions.AddRange(parameterDefinitions);
        context.Jobs.Add(job);
        await context.SaveChangesAsync();
        
        var parameters = new List<JobParameterValueDto>
        {
            new() { Name = "IntParam", Value = "NotAnInteger" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => jobParameterService.SetJobParameterValuesAsync(job.Id, parameters));

        Assert.StartsWith("Could not convert 'NotAnInteger'", exception.Message);
    }

    [Fact]
    public async Task SetJobParameterValuesAsync_ThrowsException_WhenUnknownParameterProvided()
    {
        // Arrange
        using var context = CreateContext();
        var jobParameterService = CreateJobParameterService(context);
        
        var assemblyId = 1;
        var assembly = CreateTestAssembly(assemblyId, "Test Assembly");
        var assemblyVersion = CreateTestAssemblyVersion(1, "1.0.0", assemblyId, true);
        var parameterDefinitions = new List<AssemblyParameterDefinition>
        {
            CreateTestParameterDefinition(1, "ValidParam", "System.String", true, assemblyVersion.Id)
        };
        
        assemblyVersion.ParameterDefinitions = parameterDefinitions;
        assembly.Versions.Add(assemblyVersion);
        
        var job = CreateTestJob(1, "Test Job", assemblyId);
        
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(assemblyVersion);
        context.AssemblyParameterDefinitions.AddRange(parameterDefinitions);
        context.Jobs.Add(job);
        await context.SaveChangesAsync();
        
        var parameters = new List<JobParameterValueDto>
        {
            new() { Name = "ValidParam", Value = "ValidValue" },
            new() { Name = "UnknownParam", Value = "UnknownValue" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => jobParameterService.SetJobParameterValuesAsync(job.Id, parameters));

        Assert.Equal("Unknown parameters provided: UnknownParam", exception.Message);
    }

    #endregion

    #region Helper Methods

    private static Job CreateTestJob(int id, string name, bool isActive = true)
    {
        return CreateTestJob(id, name, 1, isActive);
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

    private static JobParameter CreateTestJobParameter(int id, string name, string value, int jobId)
    {
        return new JobParameter
        {
            Id = id,
            Name = name,
            Value = value,
            JobId = jobId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

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
            MainAssemblyName = "TestAssembly.dll",
            UploadedAt = DateTime.UtcNow,
            ChangeNotes = $"Change notes for {version}",
            IsActive = isActive,
            AssemblyId = assemblyId,
            ParameterDefinitions = new List<AssemblyParameterDefinition>()
        };
    }

    private static AssemblyParameterDefinition CreateTestParameterDefinition(int id, string name, string type, bool required, int assemblyVersionId)
    {
        return new AssemblyParameterDefinition
        {
            Id = id,
            Name = name,
            Type = type,
            Description = $"Description for {name}",
            DefaultValue = required ? null : "DefaultValue",
            Required = required,
            CreatedAt = DateTime.UtcNow,
            AssemblyVersionId = assemblyVersionId
        };
    }

    #endregion
} 