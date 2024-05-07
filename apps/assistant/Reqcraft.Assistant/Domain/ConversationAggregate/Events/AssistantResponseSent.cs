namespace Reqcraft.Assistant.Domain.ConversationAggregate.Events;

public record AssistantResponseSent(Guid ConversationId, string AssistantResponse, DateTime DateSent);