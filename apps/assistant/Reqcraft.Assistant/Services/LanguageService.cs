using System.Reflection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Reqcraft.Assistant.Domain;
using SharpToken;

namespace Reqcraft.Assistant.Services;

public class LanguageService(Kernel kernel): ILanguageService
{
    private static readonly GptEncoding Encoding = GptEncoding.GetEncodingForModel("gpt-4");

    public async IAsyncEnumerable<string> GenerateResponseAsync(string userPrompt, List<ChatMessage> messages)
    {
        var chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();

        var systemPrompt = await RenderSystemPrompt(new KernelArguments { { "input", userPrompt } });

        // Limit the number of tokens to 4000 to avoid hitting the OpenAI token limit.
        var systemPromptTokens = Encoding.CountTokens($"system: {systemPrompt}");
        var userPromptTokens = Encoding.CountTokens($"user: {userPrompt}\n");
        var remainingTokens = 4000 - systemPromptTokens - userPromptTokens;

        var filteredMessages = FilterChatHistory(remainingTokens, messages);
        var history = BuildChatHistory(systemPrompt, userPrompt, filteredMessages);

        var promptSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.7,
            FrequencyPenalty = 0.0,
            PresencePenalty = 0.0,
        };

        var tokens = chatCompletionService
            .GetStreamingChatMessageContentsAsync(history, executionSettings: promptSettings)
            .Where(x => !string.IsNullOrEmpty(x.Content))
            .Select(x => x.Content!);

        await foreach (var token in tokens)
        {
            yield return token;
        }
    }

    private ChatHistory BuildChatHistory(string systemPrompt, string userPrompt, List<ChatMessage> messages)
    {
        var history = new ChatHistory();

        history.AddSystemMessage(systemPrompt);

        foreach (var message in messages)
        {
            if (message.Role == ChatRole.Assistant)
            {
                history.AddAssistantMessage(message.Content);
            }
            else
            {
                history.AddUserMessage(message.Content);
            }
        }

        history.AddUserMessage(userPrompt);

        return history;
    }

    private List<ChatMessage> FilterChatHistory(int remainingTokens, List<ChatMessage> messages)
    {
        var results = new List<ChatMessage>();

        // Iterate the list in reverse order so we add the most recent messages first.
        // We consider the most recent messages the most relevant to the conversation.
        for (var index = messages.Count - 1; index > 0; index--)
        {
            var tokenCount = Encoding.CountTokens(messages[index].Content);

            if (remainingTokens - tokenCount >= 0)
            {
                results.Add(messages[index]);
            }
        }

        // Reverse the list of messages, so they are in chronological order.
        results.Reverse();

        return results;
    }

    private async Task<string> RenderSystemPrompt(KernelArguments arguments)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Reqcraft.Assistant.SystemPrompt.txt";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);

        string promptTemplateContent = await reader.ReadToEndAsync();

        KernelPromptTemplateFactory promptTemplateFactory = new();
        var promptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(promptTemplateContent));

        return await promptTemplate.RenderAsync(kernel, arguments);
    }
}