# PuddleJobs

A .NET job scheduling platform built with **ASP.NET Core**, **Quartz.NET**, **Entity Framework Core**, and **Serilog**. Designed for cloud-native deployment with **.NET Aspire** and SQL Server.

## Features

- **Job Scheduling:** Schedule, manage, and execute jobs using Quartz.NET.
- **Assembly Versioning:** Upload and manage multiple versions of job assemblies.
- **Parameter Management:** Strongly-typed, validated job parameters with support for defaults and required fields.
- **.NET Aspire Integration:** Out-of-the-box support for distributed/cloud-native deployment, including managed SQL Server containers.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Docker (for local SQL Server via Aspire)
- (Optional) Visual Studio 2022+ or VS Code

### Running Locally

1. **Start the distributed app:**
   ```sh
   dotnet run --project PuddleJobs.AppHost
   ```
This will:
   - Start a SQL Server container (via Aspire)
   - Start the API and Web frontend, wiring up all dependencies

2. **Access the API:**
   - Swagger UI: [http://localhost:5000/swagger](http://localhost:5000/swagger) (or as shown in console output)

3. **Access the Web Frontend:**
   - [http://localhost:5001](http://localhost:5001) (or as shown in console output)

### Creating a Job
```cs
using Microsoft.Extensions.Logging;
using PuddleJobs.Core;
using Quartz;

namespace TestingApp;

[JobParameter("ThingToPrint", typeof(string), Description = "The string that will be printed", Required = true)]
public class ConsoleJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        var printThis = context.JobDetail.JobDataMap["ThingToPrint"].ToString();
        var logger = (ILogger)context.JobDetail.JobDataMap["Logger"];
        
        logger.LogInformation("{ParameterValue}", printThis);
        
        return Task.CompletedTask;
    }
}

```
The above shows a simple job with one required parameter.
 - It grabs the parameter value from the JobDataMap and prints it using an ILogger provided by PuddleJobs.
 - Logger is a reserved word that PuddleJobs uses to provide an ILogger to the job.
 - Jobs are assumed successful if an exception isn't thrown.

### Getting it scheduled
1. Publish your job project and zip the output.
2. Call the `POST` `/api/Assemblies` endpoint to upload your zipped assembly.
3. Call the `POST` `/api/Schedules` endpoint to create a cron schedule.
4. Call the `POST` `/api/Jobs` endpoint to create your job.
   - Reference the assembly and the schedule you just created.
   - Make sure to pass any parameters defined in the assembly or this will fail!

## Implementation Details
### Logging

- **Serilog** is used for all logging.
- Logs are written to:
  - **Console** (with class and assembly name context)
  - **SQL Server** (`Logs` table in the `jobscheduler` database)
- The logging pipeline is initialized in two stages:
  1. **Bootstrap logger**: Console-only, used during startup.
  2. **Full logger**: After database is ready, logs to both console and SQL Server.

### Project Structure

```text
PuddleJobs/
  PuddleJobs.ApiService/      # Main ASP.NET Core Web API (Quartz, EF Core, Serilog)
  PuddleJobs.Web/             # Blazor Web frontend
  PuddleJobs.ServiceDefaults/ # Aspire service defaults (telemetry, health, etc.)
  PuddleJobs.AppHost/         # Aspire distributed app host (orchestration)
  PuddleJobs.Core/            # JobParameter attribute
  PuddleJobs.Tests/           # xUnit test suite
```

## Testing
```sh
dotnet test PuddleJobs.Tests
```
