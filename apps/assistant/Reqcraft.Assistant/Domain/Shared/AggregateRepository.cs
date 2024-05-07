using Marten;
using Marten.Linq.SoftDeletes;

namespace Reqcraft.Assistant.Domain.Shared;

public class AggregateRepository(IDocumentStore documentStore)
{
    public async Task SaveAsync(AggregateRoot entity, CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.LightweightSerializableSessionAsync(cancellationToken);

        session.Events.Append(entity.Id, entity.Version, entity.PendingDomainEvents);
        await session.SaveChangesAsync(cancellationToken);
        
        entity.CommitPendingDomainEvents();
    }

    public async Task<T> LoadAsync<T>(Guid id, long? version = null, CancellationToken cancellationToken = default) where T: AggregateRoot
    {       
        await using var session = await documentStore.LightweightSerializableSessionAsync(cancellationToken);
        var aggregate = await session.Events.AggregateStreamAsync<T>(id, version ?? 0L, token: cancellationToken);

        return aggregate ?? throw new AggregateNotFoundException();
    }
}