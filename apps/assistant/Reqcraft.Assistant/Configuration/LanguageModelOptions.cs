namespace Reqcraft.Assistant.Configuration;

public class LanguageModelOptions
{
    public string Endpoint { get; set; }
    public string ChatDeploymentName { get; set; }
    public string EmbeddingDeploymentName { get; set; }
    public string ApiKey { get; set; }
}