using Google.Protobuf.Collections;
using Marten;
using Microsoft.SemanticKernel.Memory;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Reqcraft.Assistant.Services;

public class ApplicationMemoryStore(QdrantClient client) : IMemoryStore
{
    public async Task CreateCollectionAsync(string collectionName,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await client.CreateCollectionAsync(collectionName, new VectorParams
        {
            Distance = Distance.Cosine,
            Size = 1536
        }, cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<string> GetCollectionsAsync(
        CancellationToken cancellationToken = new CancellationToken())
    {
        var collections = await client.ListCollectionsAsync(cancellationToken: cancellationToken);

        foreach (string collectionName in collections)
        {
            yield return collectionName;
        }
    }

    public async Task<bool> DoesCollectionExistAsync(string collectionName,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return await client.CollectionExistsAsync(collectionName, cancellationToken);
    }

    public async Task DeleteCollectionAsync(string collectionName,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await client.DeleteCollectionAsync(collectionName, cancellationToken: cancellationToken);
    }

    public async Task<string> UpsertAsync(string collectionName, MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        var qdrantRecord = await ConvertFromMemoryRecordAsync(collectionName, record);
        var pointStruct = new PointStruct
        {
            Id = new PointId { Uuid = qdrantRecord.PointId },
            Vectors = qdrantRecord.Embedding
        };

        foreach (var (key, value) in qdrantRecord.GetPointPayload())
        {
            pointStruct.Payload[key] = value;
        }

        await client.UpsertAsync(collectionName, [pointStruct], cancellationToken: cancellationToken);

        return qdrantRecord.PointId;
    }

    public async IAsyncEnumerable<string> UpsertBatchAsync(string collectionName, IEnumerable<MemoryRecord> records,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var storeRecords = await Task
            .WhenAll(records.Select(async record => await ConvertFromMemoryRecordAsync(collectionName, record)))
            .ConfigureAwait(false);

        var pointStructs = storeRecords.Select(qdrantRecord =>
        {
            var pointStruct = new PointStruct
            {
                Id = new PointId { Uuid = qdrantRecord.PointId },
                Vectors = qdrantRecord.Embedding,
            };

            foreach (var (key, value) in qdrantRecord.GetPointPayload())
            {
                pointStruct.Payload[key] = value;
            }

            return pointStruct;
        }).ToList();

        await client.UpsertAsync(collectionName, pointStructs, cancellationToken: cancellationToken);

        foreach (var record in storeRecords)
        {
            yield return record.PointId;
        }
    }

    public async Task<MemoryRecord?> GetAsync(string collectionName, string key, bool withEmbedding = false,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await client.RetrieveAsync(collectionName, Guid.Parse(key), withPayload: true,
            withVectors: withEmbedding, cancellationToken: cancellationToken);

        if (result.Count == 0)
        {
            return null;
        }

        var metadata = new MemoryRecordMetadata(
            isReference: result[0].Payload["IsReference"].BoolValue,
            id: result[0].Payload["Id"].StringValue,
            text: result[0].Payload["Text"].StringValue,
            description: result[0].Payload["Description"].StringValue,
            externalSourceName: result[0].Payload["ExternalSourceName"].StringValue,
            additionalMetadata: result[0].Payload["AdditionalMetadata"].StringValue);

        var memoryRecord = new MemoryRecord(
            metadata: metadata,
            embedding: new ReadOnlyMemory<float>(result[0].Vectors.Vector.Data.ToArray()),
            key: result[0].Id.Uuid);

        return memoryRecord;
    }

    public async IAsyncEnumerable<MemoryRecord> GetBatchAsync(string collectionName, IEnumerable<string> keys,
        bool withEmbeddings = false,
        CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var key in keys)
        {
            var record = await GetAsync(
                collectionName, key, withEmbedding: withEmbeddings,
                cancellationToken: cancellationToken);

            if (record != null)
            {
                yield return record;
            }
        }
    }

    public async Task RemoveAsync(string collectionName, string key,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await client.DeleteAsync(collectionName, Guid.Parse(key), cancellationToken: cancellationToken);
    }

    public async Task RemoveBatchAsync(string collectionName, IEnumerable<string> keys,
        CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var key in keys)
        {
            await RemoveAsync(collectionName, key, cancellationToken: cancellationToken);
        }
    }

    public async IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesAsync(string collectionName,
        ReadOnlyMemory<float> embedding, int limit,
        double minRelevanceScore = 0, bool withEmbeddings = false,
        CancellationToken cancellationToken = new CancellationToken())
    {
        WithPayloadSelector? payloadSelection = new WithPayloadSelector
        {
            Include = new PayloadIncludeSelector
            {
            }
        };

        payloadSelection.Include.Fields.AddRange([
            "Id", "Description", "Text", "ExternalSourceName", "IsReference", "AdditionalMetadata"
        ]);

        var results = await client.SearchAsync(
            collectionName, embedding, limit: 3, scoreThreshold: (float)minRelevanceScore,
            payloadSelector: payloadSelection, cancellationToken: cancellationToken);

        foreach (var result in results)
        {
            var recordMetadata = new MemoryRecordMetadata(
                isReference: result.Payload["IsReference"].BoolValue,
                id: result.Payload["Id"].StringValue,
                text: result.Payload["Text"].StringValue,
                description: result.Payload["Description"].StringValue,
                externalSourceName: result.Payload["ExternalSourceName"].StringValue,
                additionalMetadata: result.Payload["AdditionalMetadata"].StringValue);

            var memoryRecord = new MemoryRecord(metadata: recordMetadata, embedding: result.Vectors.Vector.Data.ToArray(),
                key: result.Id.Uuid.ToString());

            yield return (memoryRecord, result.Score);
        }
    }

    public Task<(MemoryRecord, double)?> GetNearestMatchAsync(string collectionName, ReadOnlyMemory<float> embedding,
        double minRelevanceScore = 0,
        bool withEmbedding = false, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    private async Task<QdrantMemoryRecord> ConvertFromMemoryRecordAsync(string collectionName, MemoryRecord record)
    {
        string pointId = "";

        if (!string.IsNullOrEmpty(record.Key))
        {
            pointId = record.Key;
        }
        else
        {
            var point = await GetPointByMetadataIdAsync(collectionName, record.Metadata.Id);

            if (point != null)
            {
                pointId = point.Id.ToString();
            }
            else
            {
                pointId = Guid.NewGuid().ToString();
            }
        }

        return new QdrantMemoryRecord
        {
            PointId = pointId,
            Payload = new()
            {
                ["Id"] = record.Metadata.Id,
                ["Description"] = record.Metadata.Description,
                ["Text"] = record.Metadata.Text,
                ["ExternalSourceName"] = record.Metadata.ExternalSourceName,
                ["IsReference"] = record.Metadata.IsReference,
                ["AdditionalMetadata"] = record.Metadata.AdditionalMetadata
            },
            Embedding = record.Embedding.ToArray()
        };
    }

    private async Task<ScoredPoint?> GetPointByMetadataIdAsync(string collectionName, string metadataId,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var filter = new Filter();
        filter.Must.Add(new Condition
        {
            Field = new FieldCondition
            {
                Key = "id",
                Match = new Match
                {
                    Text = metadataId
                }
            }
        });

        var existingRecords = await client.SearchAsync(
            collectionName,
            new ReadOnlyMemory<float>(),
            filter: filter,
            limit: 1,
            cancellationToken: cancellationToken);

        return existingRecords.FirstOrDefault();
    }
}