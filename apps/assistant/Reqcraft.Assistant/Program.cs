using System.Data.Common;
using Marten;
using Marten.Events.Projections;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using Npgsql;
using Polly;
using Polly.Registry;
using Polly.Retry;
using Reqcraft.Assistant.Components;
using Reqcraft.Assistant.Domain.Shared;
using Reqcraft.Assistant.Services;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);
 
builder.AddServiceDefaults();
builder.AddQdrantClient("vector-db");

builder.Services.AddNpgsqlDataSource(builder.Configuration.GetConnectionString("assistant-db")!);
builder.Services.AddMarten(options =>
{
    options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
    options.Projections.Add<ConversationHistoryProjection>(ProjectionLifecycle.Inline);
}).UseNpgsqlDataSource();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery();

var languageModelConnectionString = new DbConnectionStringBuilder
{
    ConnectionString = builder.Configuration.GetConnectionString("language-model")
};

var kernel = builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(
        "gpt-40",
        languageModelConnectionString["Endpoint"].ToString()!,
        languageModelConnectionString["Key"].ToString()!)
    .AddAzureOpenAITextEmbeddingGeneration(
        "embedding",
        languageModelConnectionString["Endpoint"].ToString()!,
        languageModelConnectionString["Key"].ToString()!);

// Configure the memory plugin with associated services.
// This is a workaround for the fact that Microsoft's version of the qdrant connector doesn't support API keys.
kernel.Plugins.AddFromType<TextMemoryPlugin>("memory");
kernel.Services.AddTransient<ISemanticTextMemory, SemanticTextMemory>();
kernel.Services.AddTransient<IMemoryStore, ApplicationMemoryStore>();
kernel.Services.AddSingleton<IFunctionInvocationFilter, MemoryInvocationFilter>();

builder.Services.AddScoped<AggregateRepository>();

builder.Services.AddScoped<IConversationHistoryService, ConversationHistoryService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<ApplicationMemoryStore>();

builder.Services.AddResiliencePipeline("RetryMigrations", policy =>
{
    policy.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1)
    });
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

using var scope = app.Services.CreateScope();
var resiliencePipelineProvider = scope.ServiceProvider.GetRequiredService<ResiliencePipelineProvider<string>>();
var resiliencePipeline = resiliencePipelineProvider.GetPipeline("RetryMigrations");

// Make sure to create the database when the application starts.
// We may need to retry this operation if the database is not available yet.
resiliencePipeline.Execute(() =>
{
    var serverDbConnection = new NpgsqlConnectionStringBuilder(app.Configuration.GetConnectionString("assistant-db"));
    var databaseName = serverDbConnection.Database;
    
    serverDbConnection.Database = "";

    using var databaseConnection = new NpgsqlConnection(serverDbConnection.ConnectionString);
    
    var findDatabaseCommand = databaseConnection.CreateCommand(
        $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'");
    
    var createDatabaseCommand = databaseConnection.CreateCommand($"CREATE DATABASE \"{databaseName}\"");
    
    var grantAllCommand = databaseConnection.CreateCommand(
        $"GRANT ALL PRIVILEGES ON DATABASE \"{databaseName}\" TO postgres");

    databaseConnection.Open();

    var databaseExists = findDatabaseCommand.ExecuteScalar() != null;

    if (!databaseExists)
    {
        createDatabaseCommand.ExecuteNonQuery();
        grantAllCommand.ExecuteNonQuery();
    }
});

app.Run();
