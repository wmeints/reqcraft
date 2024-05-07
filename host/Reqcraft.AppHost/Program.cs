using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var vectorDatabaseApiKey = builder.AddParameter("VectorDatabaseApiKey", secret: true);
var postgresDatabasePassword = builder.AddParameter("PostgresDatabasePassword", secret: true);

var applicationInsights = builder.ExecutionContext.IsPublishMode ? 
    builder.AddAzureApplicationInsights("app-insights") : 
    builder.AddConnectionString("app-insights");

var databaseServer = builder.AddPostgres("postgres", password: postgresDatabasePassword)
    .WithDataVolume()
    .WithInitBindMount("../../data/postgres/init");

var assistantDb = databaseServer.AddDatabase("assistant-db");

var vectorDatabase = builder.AddQdrant("vector-db", apiKey: vectorDatabaseApiKey)
    .WithDataVolume();

IResourceBuilder<IResourceWithConnectionString> languageModel;

if (builder.ExecutionContext.IsPublishMode)
{
    var azureLanguageModelResource = builder.AddAzureOpenAI("language-model");
    
    azureLanguageModelResource.AddDeployment(new AzureOpenAIDeployment("gpt-40", "gpt-4", "1106-Preview"));
    azureLanguageModelResource.AddDeployment(new AzureOpenAIDeployment("embedding", "text-embedding-ada-002", "2"));

    languageModel = azureLanguageModelResource;
}
else
{
    languageModel = builder.AddConnectionString("language-model");
}

builder.AddProject<Reqcraft_Assistant>("assistant")
    .WithReference(vectorDatabase)
    .WithReference(assistantDb)
    .WithReference(languageModel)
    .WithReference(applicationInsights);

builder.Build().Run();
