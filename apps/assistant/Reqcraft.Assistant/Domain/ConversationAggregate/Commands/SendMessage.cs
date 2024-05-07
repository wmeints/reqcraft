namespace Reqcraft.Assistant.Domain.ConversationAggregate.Commands;

public record SendMessage(Guid ConversationId, string UserPrompt);