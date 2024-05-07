using Reqcraft.Assistant.Domain;

namespace Reqcraft.Assistant.Services;

public interface ILanguageService
{
    IAsyncEnumerable<string> GenerateResponseAsync(string userPrompt, List<ChatMessage> messages);
}