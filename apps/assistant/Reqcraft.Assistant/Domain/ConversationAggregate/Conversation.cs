using System.Diagnostics;
using System.Text;
using Reqcraft.Assistant.Domain.ConversationAggregate.Commands;
using Reqcraft.Assistant.Domain.ConversationAggregate.Events;
using Reqcraft.Assistant.Domain.Shared;
using Reqcraft.Assistant.Services;

namespace Reqcraft.Assistant.Domain.ConversationAggregate;

public class Conversation : AggregateRoot
{
    private readonly List<ChatMessage> _messages = new();

    private Conversation()
    {
    }

    public DateTime DateStarted { get; private set; }
    public DateTime? DateModified { get; private set; }
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();

    public static Conversation Start(StartConversation cmd)
    {
        var conversation = new Conversation();
        conversation.EmitDomainEvent(new ConversationStarted(cmd.ConversationId, DateTime.UtcNow));

        return conversation;
    }

    public SendMessageResult SendMessage(SendMessage cmd, ILanguageService languageService)
    {
        EmitDomainEvent(new UserMessageReceived(cmd.ConversationId, cmd.UserPrompt, DateTime.UtcNow));

        var languageModelResponseStream = ProcessLanguageModelResponse(
            languageService.GenerateResponseAsync(cmd.UserPrompt, _messages.SkipLast(1).ToList()));

        return new SendMessageResult(languageModelResponseStream);

        async IAsyncEnumerable<string> ProcessLanguageModelResponse(IAsyncEnumerable<string> responseStream)
        {
            var responseContentBuilder = new StringBuilder();

            await foreach (var chunk in responseStream)
            {
                responseContentBuilder.Append(chunk);
                yield return chunk;
            }

            EmitDomainEvent(new AssistantResponseSent(Id, responseContentBuilder.ToString(), DateTime.UtcNow));
        }
    }

    private void Apply(UserMessageReceived userMessageReceived)
    {
        _messages.Add(new ChatMessage(
            userMessageReceived.DateReceived,
            ChatRole.User,
            userMessageReceived.UserPrompt));
        
        DateModified = userMessageReceived.DateReceived;
    }

    private void Apply(AssistantResponseSent assistantResponseSent)
    {
        _messages.Add(new ChatMessage(
            assistantResponseSent.DateSent,
            ChatRole.Assistant,
            assistantResponseSent.AssistantResponse));

        DateModified = assistantResponseSent.DateSent;
    }

    private void Apply(ConversationStarted conversationStarted)
    {
        Id = conversationStarted.ConversationId;
        DateStarted = conversationStarted.DateStarted;
    }

    protected override bool TryApplyDomainEvent(object domainEvent)
    {
        switch (domainEvent)
        {
            case ConversationStarted conversationStarted:
                Apply(conversationStarted);
                break;
            case UserMessageReceived userMessageReceived:
                Apply(userMessageReceived);
                break;
            case AssistantResponseSent assistantResponseSent:
                Apply(assistantResponseSent);
                break;
            default:
                return false;
        }

        return true;
    }
}