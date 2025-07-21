using PuddleJobs.Core.DTOs;
using PuddleJobs.ApiService.Models;

namespace PuddleJobs.Tests.TestHelpers;

public static class TestDataBuilder
{
    public static JobDto CreateJobDto(int id = 1, string name = "Test Job", bool isActive = true)
    {
        return new JobDto
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            IsActive = isActive,
            AssemblyId = 1,
            CreatedAt = DateTime.UtcNow,
            Schedules = new List<ScheduleDto>()
        };
    }

    public static CreateJobDto CreateCreateJobDto(string name = "Test Job", int assemblyId = 1)
    {
        return new CreateJobDto
        {
            Name = name,
            Description = $"Description for {name}",
            AssemblyId = assemblyId,
            ScheduleIds = new List<int> { 1, 2 },
            Parameters = new List<JobParameterValueDto>
            {
                new() { Name = "Param1", Value = "Value1" },
                new() { Name = "Param2", Value = "Value2" }
            }
        };
    }

    public static UpdateJobDto CreateUpdateJobDto(string name = "Updated Job", bool isActive = false)
    {
        return new UpdateJobDto
        {
            Name = name,
            Description = $"Updated description for {name}",
            IsActive = isActive,
            ScheduleIds = new List<int> { 1, 3 },
            Parameters = new List<JobParameterValueDto>
            {
                new() { Name = "Param1", Value = "UpdatedValue1" },
                new() { Name = "Param2", Value = "UpdatedValue2" }
            }
        };
    }

    public static JobParameterDto CreateJobParameterDto(int id = 1, string name = "TestParam", string value = "TestValue", int jobId = 1)
    {
        return new JobParameterDto
        {
            Id = id,
            Name = name,
            Value = value,
            JobId = jobId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    public static CreateJobParameterDto CreateCreateJobParameterDto(string name = "TestParam", string value = "TestValue")
    {
        return new CreateJobParameterDto
        {
            Name = name,
            Value = value
        };
    }

    public static AssemblyParameterDefintionDto[] CreateJobParameterInfos()
    {
        return new[]
        {
            new AssemblyParameterDefintionDto
            {
                Name = "RequiredParam",
                Type = "System.String",
                Description = "A required string parameter",
                Required = true,
                DefaultValue = null
            },
            new AssemblyParameterDefintionDto
            {
                Name = "OptionalParam",
                Type = "System.Int32",
                Description = "An optional integer parameter",
                Required = false,
                DefaultValue = "42"
            }
        };
    }

    public static JobParameterValueDto[] CreateJobParameterValues()
    {
        return new[]
        {
            new JobParameterValueDto { Name = "Param1", Value = "Value1" },
            new JobParameterValueDto { Name = "Param2", Value = "Value2" }
        };
    }

    public static ScheduleDto CreateScheduleDto(int id = 1, string name = "Test Schedule")
    {
        return new ScheduleDto
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            CronExpression = "0 0 * * * ?",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static CreateScheduleDto CreateCreateScheduleDto(string name = "Test Schedule")
    {
        return new CreateScheduleDto
        {
            Name = name,
            Description = $"Description for {name}",
            CronExpression = "0 0 * * * ?"
        };
    }

    public static UpdateScheduleDto CreateUpdateScheduleDto(string description = "Updated Description", bool isActive = false)
    {
        return new UpdateScheduleDto
        {
            Description = description,
            CronExpression = "0 0 12 * * ?",
            IsActive = isActive
        };
    }

    public static CronValidationResult CreateCronValidationResult(bool isValid = true, string? errorMessage = null)
    {
        return isValid 
            ? CronValidationResult.Success() 
            : CronValidationResult.Failure(errorMessage ?? "Invalid cron expression");
    }

    public static List<DateTime> CreateNextExecutionTimes(int count = 5)
    {
        return Enumerable.Range(1, count)
            .Select(i => DateTime.UtcNow.AddHours(i))
            .ToList();
    }

    public static AssemblyDto CreateAssemblyDto(int id = 1, string name = "Test Assembly")
    {
        return new AssemblyDto
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static CreateAssemblyDto CreateCreateAssemblyDto(string name = "Test Assembly")
    {
        return new CreateAssemblyDto
        {
            Name = name,
            Description = $"Description for {name}",
            MainAssemblyName = "TestingApp.dll"
        };
    }

    public static CreateAssemblyVersionDto CreateCreateAssemblyVersionDto(string version = "1.0.0")
    {
        return new CreateAssemblyVersionDto
        {
            Version = version,
            MainAssemblyName = "TestingApp.dll",
            ChangeNotes = $"Version {version} release notes"
        };
    }

    public static byte[] CreateMockZipContent(int size = 1024)
    {
        var random = new Random();
        var content = new byte[size];
        random.NextBytes(content);
        return content;
    }
} 