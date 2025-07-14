using Microsoft.EntityFrameworkCore;
using PuddleJobs.ApiService.Data;
using Microsoft.Extensions.Logging;

namespace PuddleJobs.ApiService.Services;

public class DatabaseInitializationService
{
    private readonly JobSchedulerDbContext _context;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(JobSchedulerDbContext context, ILogger<DatabaseInitializationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Starting database initialization...");

        var created = await _context.Database.EnsureCreatedAsync();

        if (created) 
        {
            _logger.LogInformation("Job scheduler tables created successfully.");
        }

        _logger.LogInformation("Database initialization completed successfully.");
    }
} 