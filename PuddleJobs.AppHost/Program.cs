var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("jobscheduler");

var apiService = builder.AddProject<Projects.PuddleJobs_ApiService>("apiservice")
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

builder.AddProject<Projects.PuddleJobs_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
