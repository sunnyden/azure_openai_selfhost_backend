using OpenAISelfhost.DataContracts.Request.Chat;
using OpenAISelfhost.DataContracts.Response.Chat;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenAISelfhost.Service.OpenAI.Utils
{
    public class GPT4VisionClient
    {
        private readonly string endpoint;
        private readonly string key;
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        public GPT4VisionClient(string endpoint, string deployment, string key)
        {
            this.endpoint = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2023-12-01-preview";
            this.key = key;
        }
        public async Task<ChatResponse> RequestCompletion(ChatCompletionRequest request)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", key);
            Console.WriteLine(JsonSerializer.Serialize(request, jsonSerializerOptions));

            var response = await client.PostAsync(
                endpoint, 
                new StringContent(JsonSerializer.Serialize(request, jsonSerializerOptions), Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonNode>(await response.Content.ReadAsStringAsync());
                var chatResponse = new ChatResponse()
                {
                    Message = (string)result?["choices"]?[0]?["message"]?["content"],
                    PromptTokens = (int)result["usage"]["prompt_tokens"],
                    ResponseTokens = (int)result["usage"]["completion_tokens"],
                    TotalTokens = (int)result["usage"]["total_tokens"],
                    StopReason = (string)result?["choices"]?[0]?["finish_details"]?["type"]
                };
                return chatResponse;
            }
            else
            {
                throw new Exception($"OpenAI request failed with status code {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
}
