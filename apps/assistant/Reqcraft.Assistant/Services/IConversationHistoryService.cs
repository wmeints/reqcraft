using Reqcraft.Assistant.Domain.ConversationHistory;

namespace Reqcraft.Assistant.Services;

public interface IConversationHistoryService
{
    Task<IEnumerable<HistoryRecord>> GetConversationHistoryAsync();
}