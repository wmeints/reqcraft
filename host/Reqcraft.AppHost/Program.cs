using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddPostgres("postgres");
var assistantDb = databaseServer.AddDatabase("assistantDb");

var vectorDatabase = builder.AddQdrant("vectorDb");

builder.AddProject<Reqcraft_Assistant>("assistant")
    .WithReference(vectorDatabase)
    .WithReference(assistantDb);

builder.Build().Run();
