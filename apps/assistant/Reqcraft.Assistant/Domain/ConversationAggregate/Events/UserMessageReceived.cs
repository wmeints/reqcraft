namespace Reqcraft.Assistant.Domain.ConversationAggregate.Events;

public record UserMessageReceived(Guid ConversationId, string UserPrompt, DateTime DateReceived);