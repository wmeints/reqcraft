using Marten.Events.Projections;
using Reqcraft.Assistant.Domain.ConversationAggregate.Events;
using Reqcraft.Assistant.Domain.ConversationHistory;

namespace Reqcraft.Assistant.Services;

public class ConversationHistoryProjection: EventProjection
{
    public ConversationHistoryProjection()
    {
        Project<ConversationStarted>((domainEvent, operations) =>
        {
            operations.Store(new HistoryRecord
            {
                Id = Guid.NewGuid(),
                Date = domainEvent.DateStarted,
                ConversationId = domainEvent.ConversationId
            });
        });

        Project<AssistantResponseSent>((domainEvent, operations) =>
        {
            var historyRecord = operations
                .Query<HistoryRecord>()
                .FirstOrDefault(record => record.ConversationId == domainEvent.ConversationId);

            if (historyRecord == null) return;
            
            historyRecord.Date = domainEvent.DateSent;
            operations.Store(historyRecord);
        });
    }
}