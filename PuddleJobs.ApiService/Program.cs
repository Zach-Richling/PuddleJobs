using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PuddleJobs.ApiService.Data;
using PuddleJobs.ApiService.Enrichers;
using PuddleJobs.ApiService.Extensions;
using PuddleJobs.ApiService.Services;
using Quartz;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Data;
using System.IO.Abstractions;

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

builder.Logging
    .ClearProviders()
    .AddSerilog();

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithAuth(builder.Configuration);

builder.Services.AddDbContext<JobSchedulerDbContext>(options =>
{
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Keycloak:ValidIssuer"],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PuddleJobs API v1");
    });

    app.MapOpenApi();

    app.MapGet("/index.html", context =>
    {
        context.Response.Redirect("/swagger");
        return Task.CompletedTask;
    });
}

// Initialize database, database logging, and scheduler
using (var scope = app.Services.CreateScope())
{
    var dbInitService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
    await dbInitService.InitializeDatabaseAsync();

    var logColumns = new ColumnOptions()
    {
        AdditionalColumns = new[]
        {
            new SqlColumn() { ColumnName = "ClassName" },
            new SqlColumn() { ColumnName = "FireInstanceId", DataType = SqlDbType.BigInt  }
        }
    };

    logColumns.Store.Add(StandardColumn.LogEvent);
    logColumns.Store.Remove(StandardColumn.Properties);
    logColumns.TimeStamp.DataType = SqlDbType.DateTime2;

    var loggerConfig = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.With<NameEnricher>()
        .WriteTo.MSSqlServer(
            connectionString: builder.Configuration.GetConnectionString("jobscheduler"),
            sinkOptions: new MSSqlServerSinkOptions
            {
                TableName = "Logs",
                AutoCreateSqlTable = true
            },
            columnOptions: logColumns
        );

    if (app.Environment.IsDevelopment())
    {
        loggerConfig.WriteTo.Console(outputTemplate: consoleMessageTemplate);
    }

    Log.Logger = loggerConfig.CreateLogger();

    var jobSchedulerService = scope.ServiceProvider.GetRequiredService<IJobSchedulerService>();
    await jobSchedulerService.InitializeSchedulerAsync();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowAllOrigins");

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
