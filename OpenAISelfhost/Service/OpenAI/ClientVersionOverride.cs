
using Azure.AI.Inference;

namespace OpenAISelfhost.Service.OpenAI
{
    public class ClientVersionOverride : AzureAIInferenceClientOptions
    {
        internal string Version { get; } = "2024-11-01-preview";
    }
}
