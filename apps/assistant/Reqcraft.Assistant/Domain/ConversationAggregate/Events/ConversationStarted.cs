namespace Reqcraft.Assistant.Domain.ConversationAggregate.Events;

public record ConversationStarted(Guid ConversationId, DateTime DateStarted);