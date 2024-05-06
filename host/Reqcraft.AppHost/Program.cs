using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var vectorDatabaseApiKey = builder.AddParameter("VectorDatabaseApiKey", secret: true);
var postgresDatabasePassword = builder.AddParameter("PostgresDatabasePassword", secret: true);

var applicationInsights = builder.ExecutionContext.IsPublishMode ? 
    builder.AddAzureApplicationInsights("app-insights") : 
    builder.AddConnectionString("app-insights");

var databaseServer = builder.AddPostgres("postgres", password: postgresDatabasePassword);
var assistantDb = databaseServer.AddDatabase("assistant-db");

var vectorDatabase = builder.AddQdrant("vector-db", apiKey: vectorDatabaseApiKey);
var languageModel = builder.AddConnectionString("language-model");

var assistant = builder.AddProject<Reqcraft_Assistant>("assistant")
    .WithReference(vectorDatabase)
    .WithReference(assistantDb)
    .WithReference(languageModel)
    .WithReference(applicationInsights);

builder.Build().Run();
