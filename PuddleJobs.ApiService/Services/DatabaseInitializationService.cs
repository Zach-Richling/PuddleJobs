using Microsoft.EntityFrameworkCore;
using PuddleJobs.ApiService.Data;
using Microsoft.Extensions.Logging;

namespace PuddleJobs.ApiService.Services;

public class DatabaseInitializationService
{
    private readonly JobSchedulerDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(JobSchedulerDbContext context, IConfiguration configuration, ILogger<DatabaseInitializationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Starting database initialization...");

        // Ensure our custom tables are created
        _logger.LogInformation("Creating job scheduler tables...");
        var created = await _context.Database.EnsureCreatedAsync();

        if (created) 
        {
            _logger.LogInformation("Job scheduler tables created successfully.");
        }

        await EnsureQuartzTablesExistAsync();
        
        _logger.LogInformation("Database initialization completed successfully.");
    }

    private async Task EnsureQuartzTablesExistAsync()
    {
        _logger.LogInformation("Checking if Quartz tables exist...");
        
        var connectionString = _configuration.GetConnectionString("jobscheduler");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'jobscheduler' not found.");
        }

        bool tableExists;
        try
        {
            await _context.Database.SqlQueryRaw<object>("SELECT TOP 1 1 FROM QRTZ_JOB_DETAILS").FirstOrDefaultAsync();
            tableExists = true;
            _logger.LogInformation("Quartz tables already exist.");
        }
        catch
        {
            tableExists = false;
            _logger.LogInformation("Quartz tables do not exist. They will be created.");
        }

        if (!tableExists)
        {
            var quartzSchemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "QuartzSchema.sql");
            if (File.Exists(quartzSchemaPath))
            {
                _logger.LogInformation("Creating Quartz tables...");
                var quartzSchema = await File.ReadAllTextAsync(quartzSchemaPath);
                await ExecuteSqlScriptWithGoStatementsAsync(quartzSchema);
                _logger.LogInformation("Quartz tables created successfully.");
            }
            else
            {
                throw new InvalidOperationException($"Quartz schema file not found at {quartzSchemaPath}. Please ensure the official Quartz.NET schema file is included in the application.");
            }
        }
    }

    private async Task ExecuteSqlScriptWithGoStatementsAsync(string sqlScript)
    {
        // Split the script by GO statements
        var batches = sqlScript.Split(["\r\nGO", "\nGO", "\rGO"], StringSplitOptions.RemoveEmptyEntries);

        _logger.LogInformation("Executing SQL script with {BatchCount} batches...", batches.Length);
        
        // Execute all batches within a single transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        { 
            _logger.LogInformation("Starting transaction for SQL script execution...");
            
            for (int i = 0; i < batches.Length; i++)
            {
                var trimmedBatch = batches[i].Trim();
                if (!string.IsNullOrWhiteSpace(trimmedBatch))
                {
                    _logger.LogDebug("Executing batch {BatchNumber}/{TotalBatches}", i + 1, batches.Length);
                    await _context.Database.ExecuteSqlRawAsync(trimmedBatch);
                }
            }
            
            await transaction.CommitAsync();
            _logger.LogInformation("SQL script execution completed successfully. All {BatchCount} batches executed.", batches.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SQL script. Rolling back transaction.");
            await transaction.RollbackAsync();
            throw;
        }
    }
} 