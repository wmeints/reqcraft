namespace Reqcraft.Assistant.Domain;

public record ChatMessage(DateTime Timestamp, ChatRole Role, string Content);