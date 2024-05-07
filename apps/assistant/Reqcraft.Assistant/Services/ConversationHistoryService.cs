using Marten;
using Reqcraft.Assistant.Domain.ConversationHistory;

namespace Reqcraft.Assistant.Services;

public class ConversationHistoryService(IDocumentStore store): IConversationHistoryService
{
    public async Task<IEnumerable<HistoryRecord>> GetConversationHistoryAsync()
    {
        await using var session = store.QuerySession();
        var records = await session.Query<HistoryRecord>().OrderByDescending(x => x.Date).ToListAsync();

        return records;
    }
}