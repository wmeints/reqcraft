namespace Reqcraft.Assistant.Domain.ConversationHistory;

public class HistoryRecord
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public DateTime Date { get; set; }
    public string Subject { get; set; }
}