using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Always parameterize secrets! This is a good practice to follow, even if you're not using the Azure Developer CLI.
var vectorDatabaseApiKey = builder.AddParameter("VectorDatabaseApiKey", secret: true);
var postgresDatabasePassword = builder.AddParameter("PostgresDatabasePassword", secret: true);

// Locally we use a connection string to the Azure Application Insights resource, but in production we use the Azure
// Application Insights resource, so we can deploy through the Azure Developer CLI automatically.
var applicationInsights = builder.ExecutionContext.IsPublishMode ? 
    builder.AddAzureApplicationInsights("app-insights") : 
    builder.AddConnectionString("app-insights");

// Store the data for the SQL Server on the host. This makes it possible to continue debugging without
// having to repeat setup steps. The init mount is especially useful since it contains the schema and seed data.
var databaseServer = builder.AddPostgres("postgres", password: postgresDatabasePassword)
    .WithDataVolume() // Use a volume instead of a bind so that postgres can modify permissions.
    .WithInitBindMount("../../data/postgres/init");

var assistantDb = databaseServer.AddDatabase("assistant-db");

var vectorDatabase = builder.AddQdrant("vector-db", apiKey: vectorDatabaseApiKey);

IResourceBuilder<IResourceWithConnectionString> languageModel;

// Locally we can use a connection string, but in production we use the Azure OpenAI resource so we can deploy through
// the Azure Developer CLI automatically. Note that we can't set the quota here so you may have to modify things in the
// portal after provisioning.
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
