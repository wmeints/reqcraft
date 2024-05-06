using Qdrant.Client.Grpc;

namespace Reqcraft.Assistant.Services;

public class QdrantMemoryRecord
{
    public required string PointId { get; init; }
    public Dictionary<string, object> Payload { get; init; } = new();
    public required float[] Embedding { get; init; }

    public Dictionary<string, Value> GetPointPayload()
    {
        var result = new Dictionary<string, Value>();
        
        foreach (var key in Payload.Keys)
        {
            result[key] = Payload[key] switch
            {
                string stringValue => new Value { StringValue = stringValue },
                bool boolValue => new Value { BoolValue = boolValue },
                int intValue => new Value { IntegerValue = intValue },
                float floatValue => new Value { DoubleValue = floatValue },
                _ => throw new InvalidOperationException("Invalid value type")
            };
        }

        return result;
    }
}