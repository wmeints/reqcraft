using Grpc.Core;
using Microsoft.SemanticKernel;
using Qdrant.Client.Grpc;

namespace Reqcraft.Assistant.Services;

public class MemoryInvocationFilter(Qdrant.Client.QdrantClient vectorStoreClient): IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (context.Function.Name == "Recall" && context.Function.PluginName == "memory")
        {
            try
            {
                await next(context);
            }
            catch (RpcException ex) when(ex.StatusCode == StatusCode.NotFound)
            {
                await vectorStoreClient.CreateCollectionAsync("generic", new VectorParams { Distance = Distance.Cosine, Size = 1536 });
                await next(context);
            }
        }
    }
}