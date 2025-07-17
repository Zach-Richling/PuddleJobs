var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("jobscheduler");

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume();

var apiService = builder.AddProject<Projects.PuddleJobs_ApiService>("apiservice")
    .WithReference(sqlServer)
    .WaitFor(sqlServer)
    .WithReference(keycloak)
    .WaitFor(keycloak);

builder.AddProject<Projects.PuddleJobs_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
