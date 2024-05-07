namespace Reqcraft.Assistant.Domain.Shared;

public abstract class AggregateRoot
{
    private readonly List<object> _pendingDomainEvents = new();
    public long Version { get; protected set; } = 0L;
    public Guid Id { get; protected set; }
    
    public IReadOnlyCollection<object> PendingDomainEvents => _pendingDomainEvents.AsReadOnly();

    protected void EmitDomainEvent(object domainEvent)
    {
        if (TryApplyDomainEvent(domainEvent))
        {
            _pendingDomainEvents.Add(domainEvent);
            Version++;
        }
    }

    protected abstract bool TryApplyDomainEvent(object domainEvent);
    
    public void CommitPendingDomainEvents()
    {
        _pendingDomainEvents.Clear();
    }
}