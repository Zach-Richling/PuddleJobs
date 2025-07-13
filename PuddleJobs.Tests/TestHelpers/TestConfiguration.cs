using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PuddleJobs.ApiService.DTOs;
using PuddleJobs.ApiService.Services;

namespace PuddleJobs.Tests.TestHelpers;

public static class TestConfiguration
{
    public static IServiceCollection CreateTestServices()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add mocked services
        services.AddScoped<IJobService>(provider => Mock.Of<IJobService>());
        services.AddScoped<IJobParameterService>(provider => Mock.Of<IJobParameterService>());

        return services;
    }

    public static Mock<IJobService> CreateMockJobService()
    {
        var mock = new Mock<IJobService>();
        
        // Setup default behaviors
        mock.Setup(x => x.GetAllJobsAsync())
            .ReturnsAsync(new List<JobDto>());
        
        return mock;
    }

    public static Mock<IJobParameterService> CreateMockJobParameterService()
    {
        var mock = new Mock<IJobParameterService>();
        
        // Setup default behaviors
        mock.Setup(x => x.GetJobParameterValuesAsync(It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<JobParameterDto>());
        
        return mock;
    }
} 