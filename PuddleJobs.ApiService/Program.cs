using Microsoft.EntityFrameworkCore;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.Services;
using Quartz;
using Microsoft.OpenApi.Models;
using System.IO.Abstractions;
using Serilog;
using Serilog.Events;
using PuddleJobs.ApiService.Enrichers;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

var consoleMessageTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] [{AssemblyName}] [{ClassName}]: {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.With<NameEnricher>()
    .WriteTo.Console(outputTemplate: consoleMessageTemplate)
    .CreateBootstrapLogger();

builder.AddServiceDefaults();

builder.Logging
    .ClearProviders()
    .AddSerilog();

builder.Services.AddProblemDetails();

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "PuddleJobs API", 
        Version = "v1",
        Description = "A .NET job scheduler using Quartz.NET with assembly versioning and parameter management"
    });
});

builder.Services.AddDbContext<JobSchedulerDbContext>(options =>
{
    options.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
    options.UseSqlServer(builder.Configuration.GetConnectionString("jobscheduler"),
    sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

builder.Services.AddQuartz(q => 
{
    q.UseTimeZoneConverter();
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Register application services
builder.Services.AddScoped<IAssemblyService, AssemblyService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IJobParameterService, JobParameterService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<ICronValidationService, CronValidationService>();
builder.Services.AddScoped<DatabaseInitializationService>();
builder.Services.AddScoped<IAssemblyStorageService, LocalAssemblyStorageService>();
builder.Services.AddScoped<IJobExecutionService, JobExecutionService>();
builder.Services.AddScoped<IJobSchedulerService, JobSchedulerService>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Add controller routing
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PuddleJobs API v1");
    });
    
    app.MapOpenApi();
}

// Initialize database, database logging, and scheduler
using (var scope = app.Services.CreateScope())
{
    var dbInitService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    await dbInitService.InitializeDatabaseAsync();

    var loggerConfig = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.With<NameEnricher>()
        .WriteTo.MSSqlServer(
            connectionString: builder.Configuration.GetConnectionString("jobscheduler"),
            sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
            {
                TableName = "Logs",
                AutoCreateSqlTable = true
            });

    if (app.Environment.IsDevelopment())
    {
        loggerConfig.WriteTo.Console(outputTemplate: consoleMessageTemplate);
    }

    Log.Logger = loggerConfig.CreateLogger();

    var jobSchedulerService = scope.ServiceProvider.GetRequiredService<IJobSchedulerService>();
    await jobSchedulerService.InitializeSchedulerAsync();
}

// Map controller endpoints
app.MapControllers();

app.MapDefaultEndpoints();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
