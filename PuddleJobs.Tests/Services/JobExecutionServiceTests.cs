using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.Jobs;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;
using Quartz;
using Xunit;

namespace PuddleJobs.Tests.Services;

public class JobExecutionServiceTests
{
    private static JobSchedulerDbContext CreateContext() => new(new DbContextOptionsBuilder<JobSchedulerDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AssemblyVersion CreateAssemblyVersion(int id, string mainAssemblyName = "Test.dll") => new()
    {
        Id = id,
        Version = "1.0.0",
        DirectoryPath = "/fake/path",
        MainAssemblyName = mainAssemblyName,
        UploadedAt = DateTime.UtcNow,
        IsActive = true,
        ParameterDefinitions = new List<AssemblyParameterDefinition>()
    };

    private static Job CreateJob(int id, int assemblyId, bool isActive = true) => new()
    {
        Id = id,
        Name = $"Job{id}",
        IsActive = isActive,
        AssemblyId = assemblyId,
        CreatedAt = DateTime.UtcNow,
        Parameters = new List<JobParameter>(),
        JobSchedules = new List<JobSchedule>()
    };

    private static ApiService.Models.Assembly CreateAssembly(int id, bool isDeleted = false) => new()
    {
        Id = id,
        Name = $"Assembly{id}",
        IsDeleted = isDeleted,
        CreatedAt = DateTime.UtcNow,
        Versions = new List<AssemblyVersion>(),
        Jobs = new List<Job>()
    };

    private static IJobExecutionContext CreateJobExecutionContext(int jobId)
    {
        var jobDataMap = new JobDataMap { ["jobId"] = jobId };
        var jobDetailMock = new Mock<IJobDetail>();
        jobDetailMock.SetupGet(j => j.JobDataMap).Returns(jobDataMap);
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.SetupGet(c => c.JobDetail).Returns(jobDetailMock.Object);
        contextMock.SetupGet(c => c.JobDetail.JobDataMap).Returns(jobDataMap);
        return contextMock.Object;
    }

    public class TestJob : IJob
    {
        public static List<Guid> ExecutedGuids = new();
        public Task Execute(IJobExecutionContext context)
        {
            if (context.JobDetail.JobDataMap["TestGuid"] is Guid guid)
            {
                ExecutedGuids.Add(guid);
            }
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExecuteJobAsync_HappyPath_ExecutesJobAndLogsSuccess()
    {
        //Arrange
        using var context = CreateContext();
        var logger = new Mock<ILogger<JobExecutionService>>();
        var jobLogger = new Mock<ILogger<PuddleJob>>();
        var assemblyStorage = new Mock<IAssemblyStorageService>();
        
        var assembly = CreateAssembly(1);
        var version = CreateAssemblyVersion(1);
        assembly.Versions.Add(version);
        
        var job = CreateJob(1, 1);
        job.Assembly = assembly;
        
        assembly.Jobs.Add(job);
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(version);
        context.Jobs.Add(job);
        
        await context.SaveChangesAsync();
        
        var paramDef1 = new AssemblyParameterDefinition { Id = 1, Name = "Param1", Type = typeof(string).AssemblyQualifiedName!, Required = false, AssemblyVersionId = version.Id };
        var paramDef2 = new AssemblyParameterDefinition { Id = 2, Name = "TestGuid", Type = typeof(Guid).AssemblyQualifiedName!, Required = true, AssemblyVersionId = version.Id };
        
        context.AssemblyParameterDefinitions.Add(paramDef1);
        context.AssemblyParameterDefinitions.Add(paramDef2);

        var testGuid = Guid.NewGuid();
        job.Parameters.Add(new JobParameter { Name = "Param1", Value = "TestValue", JobId = job.Id });
        job.Parameters.Add(new JobParameter { Name = "TestGuid", Value = testGuid.ToString(), JobId = job.Id });
        
        await context.SaveChangesAsync();
        
        var testJobType = typeof(TestJob);
        var mockAssembly = new Mock<System.Reflection.Assembly>();
        mockAssembly.Setup(a => a.GetTypes()).Returns(new[] { testJobType });
        assemblyStorage.Setup(s => s.LoadAssemblyVersionAsync(version, It.IsAny<AssemblyLoadContext>())).ReturnsAsync(mockAssembly.Object);

        var service = new JobExecutionService(context, assemblyStorage.Object, logger.Object, jobLogger.Object);
        var execContext = CreateJobExecutionContext(job.Id);
        
        // Act
        await service.ExecuteJobAsync(execContext);
        
        // Assert
        Assert.Contains(testGuid, TestJob.ExecutedGuids);
        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Job executed successfully")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_JobNotFound_LogsError()
    {
        //Arrange
        using var context = CreateContext();
        var logger = new Mock<ILogger<JobExecutionService>>();
        var jobLogger = new Mock<ILogger<PuddleJob>>();
        var assemblyStorage = new Mock<IAssemblyStorageService>();
        var service = new JobExecutionService(context, assemblyStorage.Object, logger.Object, jobLogger.Object);
        var execContext = CreateJobExecutionContext(999);

        //Act
        await service.ExecuteJobAsync(execContext);

        //Assert
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not start job")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_NoJobTypeInAssembly_LogsError()
    {
        // Arrange
        using var context = CreateContext();
        var logger = new Mock<ILogger<JobExecutionService>>();
        var jobLogger = new Mock<ILogger<PuddleJob>>();
        var assemblyStorage = new Mock<IAssemblyStorageService>();
        
        var assembly = CreateAssembly(1);
        var version = CreateAssemblyVersion(1);
        assembly.Versions.Add(version);
        
        var job = CreateJob(1, 1);
        job.Assembly = assembly;
        
        assembly.Jobs.Add(job);
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(version);
        context.Jobs.Add(job);

        await context.SaveChangesAsync();

        var mockAssembly = new Mock<System.Reflection.Assembly>();
        mockAssembly.Setup(a => a.GetTypes()).Returns(Array.Empty<Type>());
        assemblyStorage.Setup(s => s.LoadAssemblyVersionAsync(version, It.IsAny<AssemblyLoadContext>())).ReturnsAsync(mockAssembly.Object);
        
        var service = new JobExecutionService(context, assemblyStorage.Object, logger.Object, jobLogger.Object);
        var execContext = CreateJobExecutionContext(job.Id);

        // Act
        await service.ExecuteJobAsync(execContext);

        // Assert
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not start job")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_JobTypeInstantiationFails_LogsError()
    {
        //Act
        using var context = CreateContext();
        var logger = new Mock<ILogger<JobExecutionService>>();
        var jobLogger = new Mock<ILogger<PuddleJob>>();
        var assemblyStorage = new Mock<IAssemblyStorageService>();
        
        var assembly = CreateAssembly(1);
        var version = CreateAssemblyVersion(1);
        assembly.Versions.Add(version);
        
        var job = CreateJob(1, 1);
        job.Assembly = assembly;
        assembly.Jobs.Add(job);
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(version);
        context.Jobs.Add(job);
        
        await context.SaveChangesAsync();
        
        // Use an abstract type to force instantiation failure
        var mockAssembly = new Mock<System.Reflection.Assembly>();
        mockAssembly.Setup(a => a.GetTypes()).Returns(new[] { typeof(AbstractJob) });
        assemblyStorage.Setup(s => s.LoadAssemblyVersionAsync(version, It.IsAny<AssemblyLoadContext>())).ReturnsAsync(mockAssembly.Object);
        
        var service = new JobExecutionService(context, assemblyStorage.Object, logger.Object, jobLogger.Object);
        var execContext = CreateJobExecutionContext(job.Id);

        // Act
        await service.ExecuteJobAsync(execContext);

        // Assert
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not start job")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    public abstract class AbstractJob : IJob { public Task Execute(IJobExecutionContext context) => Task.CompletedTask; }

    [Fact]
    public async Task ExecuteJobAsync_JobExecutionThrows_LogsError()
    {
        // Arrange
        using var context = CreateContext();
        var logger = new Mock<ILogger<JobExecutionService>>();
        var jobLogger = new Mock<ILogger<PuddleJob>>();
        var assemblyStorage = new Mock<IAssemblyStorageService>();
        
        var assembly = CreateAssembly(1);
        var version = CreateAssemblyVersion(1);
        assembly.Versions.Add(version);
        
        var job = CreateJob(1, 1);
        job.Assembly = assembly;
        
        assembly.Jobs.Add(job);
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(version);
        context.Jobs.Add(job);
        
        await context.SaveChangesAsync();
        
        var mockAssembly = new Mock<System.Reflection.Assembly>();
        mockAssembly.Setup(a => a.GetTypes()).Returns(new[] { typeof(FailingJob) });
        assemblyStorage.Setup(s => s.LoadAssemblyVersionAsync(version, It.IsAny<AssemblyLoadContext>())).ReturnsAsync(mockAssembly.Object);
        
        var service = new JobExecutionService(context, assemblyStorage.Object, logger.Object, jobLogger.Object);
        var execContext = CreateJobExecutionContext(job.Id);

        // Act
        await service.ExecuteJobAsync(execContext);

        // Assert
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception during job run")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    public class FailingJob : IJob { public Task Execute(IJobExecutionContext context) => throw new InvalidOperationException("fail"); }

    [Fact]
    public async Task ExecuteJobAsync_RequiredParameterMissing_LogsError()
    {
        using var context = CreateContext();
        var logger = new Mock<ILogger<JobExecutionService>>();
        var jobLogger = new Mock<ILogger<PuddleJob>>();
        var assemblyStorage = new Mock<IAssemblyStorageService>();
        
        var assembly = CreateAssembly(1);
        var version = CreateAssemblyVersion(1);
        assembly.Versions.Add(version);
        
        var job = CreateJob(1, 1);
        job.Assembly = assembly;
        
        assembly.Jobs.Add(job);
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(version);
        context.Jobs.Add(job);

        await context.SaveChangesAsync();

        // Required parameter, no value, no default
        var paramDef = new AssemblyParameterDefinition { Id = 1, Name = "Param1", Type = typeof(string).AssemblyQualifiedName!, Required = true, AssemblyVersionId = version.Id };
        var testGuidDef = new AssemblyParameterDefinition { Id = 2, Name = "TestGuid", Type = typeof(Guid).AssemblyQualifiedName!, Required = true, AssemblyVersionId = version.Id };
        context.AssemblyParameterDefinitions.Add(paramDef);
        context.AssemblyParameterDefinitions.Add(testGuidDef);

        var testGuid = Guid.NewGuid();
        job.Parameters.Add(new JobParameter { Name = "TestGuid", Value = testGuid.ToString(), JobId = job.Id });
        
        await context.SaveChangesAsync();
        
        var mockAssembly = new Mock<System.Reflection.Assembly>();
        mockAssembly.Setup(a => a.GetTypes()).Returns(new[] { typeof(TestJob) });
        assemblyStorage.Setup(s => s.LoadAssemblyVersionAsync(version, It.IsAny<AssemblyLoadContext>())).ReturnsAsync(mockAssembly.Object);
        
        var service = new JobExecutionService(context, assemblyStorage.Object, logger.Object, jobLogger.Object);
        var execContext = CreateJobExecutionContext(job.Id);

        // Act
        await service.ExecuteJobAsync(execContext);

        // Assert
        Assert.DoesNotContain(testGuid, TestJob.ExecutedGuids);
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not start job")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_ParameterConversionFails_LogsError()
    {
        // Arrange
        using var context = CreateContext();
        var logger = new Mock<ILogger<JobExecutionService>>();
        var jobLogger = new Mock<ILogger<PuddleJob>>();
        var assemblyStorage = new Mock<IAssemblyStorageService>();
        
        var assembly = CreateAssembly(1);
        var version = CreateAssemblyVersion(1);
        assembly.Versions.Add(version);
        
        var job = CreateJob(1, 1);
        job.Assembly = assembly;
        
        assembly.Jobs.Add(job);
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(version);
        context.Jobs.Add(job);
        
        await context.SaveChangesAsync();

        // Parameter definition and value (int expected, string provided)
        var paramDef = new AssemblyParameterDefinition { Id = 1, Name = "Param1", Type = typeof(int).AssemblyQualifiedName!, Required = false, AssemblyVersionId = version.Id };
        var testGuidDef = new AssemblyParameterDefinition { Id = 2, Name = "TestGuid", Type = typeof(Guid).AssemblyQualifiedName!, Required = true, AssemblyVersionId = version.Id };
        context.AssemblyParameterDefinitions.Add(paramDef);
        context.AssemblyParameterDefinitions.Add(testGuidDef);

        var testGuid = Guid.NewGuid();
        job.Parameters.Add(new JobParameter { Name = "Param1", Value = "not_an_int", JobId = job.Id });
        job.Parameters.Add(new JobParameter { Name = "TestGuid", Value = testGuid.ToString(), JobId = job.Id });
        
        await context.SaveChangesAsync();
        
        var mockAssembly = new Mock<System.Reflection.Assembly>();
        mockAssembly.Setup(a => a.GetTypes()).Returns(new[] { typeof(TestJob) });
        assemblyStorage.Setup(s => s.LoadAssemblyVersionAsync(version, It.IsAny<AssemblyLoadContext>())).ReturnsAsync(mockAssembly.Object);
        
        var service = new JobExecutionService(context, assemblyStorage.Object, logger.Object, jobLogger.Object);
        var execContext = CreateJobExecutionContext(job.Id);

        // Act
        await service.ExecuteJobAsync(execContext);

        // Assert
        Assert.DoesNotContain(testGuid, TestJob.ExecutedGuids);
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not start job")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteJobAsync_AssemblyLoadingFails_LogsError()
    {
        // Arrange
        using var context = CreateContext();
        var logger = new Mock<ILogger<JobExecutionService>>();
        var jobLogger = new Mock<ILogger<PuddleJob>>();
        var assemblyStorage = new Mock<IAssemblyStorageService>();
        
        var assembly = CreateAssembly(1);
        var version = CreateAssemblyVersion(1);
        assembly.Versions.Add(version);
        
        var job = CreateJob(1, 1);
        job.Assembly = assembly;
        
        assembly.Jobs.Add(job);
        context.Assemblies.Add(assembly);
        context.AssemblyVersions.Add(version);
        context.Jobs.Add(job);
        
        await context.SaveChangesAsync();
        
        assemblyStorage.Setup(s => s.LoadAssemblyVersionAsync(version, It.IsAny<AssemblyLoadContext>())).ThrowsAsync(new Exception("load fail"));

        var service = new JobExecutionService(context, assemblyStorage.Object, logger.Object, jobLogger.Object);
        var execContext = CreateJobExecutionContext(job.Id);
        
        // Act
        await service.ExecuteJobAsync(execContext);
        
        // Assert
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
} 