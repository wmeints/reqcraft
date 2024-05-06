using System.Data.Common;
using Marten;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using Reqcraft.Assistant.Components;
using Reqcraft.Assistant.Services;

var builder = WebApplication.CreateBuilder(args);
 
builder.AddServiceDefaults();
builder.AddQdrantClient("vector-db");

builder.Services.AddNpgsqlDataSource("assistant-db");
builder.Services.AddMarten(options =>
{
    
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

builder.Services.AddTransient<LanguageService>();
builder.Services.AddTransient<ApplicationMemoryStore>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
