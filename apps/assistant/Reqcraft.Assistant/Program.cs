using System.Data.Common;
using Marten;
using Microsoft.SemanticKernel;
using Reqcraft.Assistant.Components;
using Reqcraft.Assistant.Services;

var builder = WebApplication.CreateBuilder(args);
 
builder.AddServiceDefaults();
builder.AddQdrantClient("vectorDb");

builder.Services.AddNpgsqlDataSource("assistantDb");
builder.Services.AddMarten(options =>
{
    
}).UseNpgsqlDataSource();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery();

var languageModelConnectionString = new DbConnectionStringBuilder
{
    ConnectionString = builder.Configuration.GetConnectionString("languageModel")
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

kernel.Services.AddSingleton<IFunctionInvocationFilter, MemoryInvocationFilter>();

builder.Services.AddTransient<LanguageService>();
builder.Services.AddTransient<ApplicationMemoryStore>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
