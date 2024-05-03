using System.Data.Common;
using Marten;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Reqcraft.Assistant.Components;
using Reqcraft.Assistant.Configuration;
using Reqcraft.Assistant.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddNpgsqlDataSource("assistantDb");
builder.Services.AddMarten(options =>
{
    
}).UseNpgsqlDataSource();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery();

var languageModelOptions = builder.Configuration.GetRequiredSection("LanguageModel").Get<LanguageModelOptions>()!;

var kernel = builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(
        languageModelOptions.ChatDeploymentName,
        languageModelOptions.Endpoint,
        languageModelOptions.ApiKey)
    .AddAzureOpenAITextEmbeddingGeneration(
        languageModelOptions.EmbeddingDeploymentName, 
        languageModelOptions.Endpoint, 
        languageModelOptions.ApiKey);

builder.Services.AddTransient<LanguageService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
