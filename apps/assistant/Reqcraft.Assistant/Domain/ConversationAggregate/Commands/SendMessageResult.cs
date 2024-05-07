namespace Reqcraft.Assistant.Domain.ConversationAggregate.Commands;

public record SendMessageResult(IAsyncEnumerable<string> ResponseStream);
