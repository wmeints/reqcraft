using Projects;

var builder = DistributedApplication.CreateBuilder(args);

//var vectorStoreApiKey = builder.AddParameter("VectorStoreApiKey", true);

var databaseServer = builder.AddPostgres("postgres");
var assistantDb = databaseServer.AddDatabase("assistantDb");

var vectorDatabase = builder.AddQdrant("vectorDb");

IResourceBuilder<IResourceWithConnectionString> languageModel;

if (builder.ExecutionContext.IsPublishMode)
{
    var languageModelAzureResource =  builder.AddAzureOpenAI("languageModel");

    languageModelAzureResource.AddDeployment(new AzureOpenAIDeployment("embedding", "text-embedding-ada-002", "2"));
    languageModelAzureResource.AddDeployment(new AzureOpenAIDeployment("gpt-40", "gpt-4", "1106-preview"));

    languageModel = languageModelAzureResource;
}
else
{
    languageModel = builder.AddConnectionString("languageModel");
}

builder.AddProject<Reqcraft_Assistant>("assistant")
    .WithReference(vectorDatabase)
    .WithReference(assistantDb)
    .WithReference(languageModel);

builder.Build().Run();
