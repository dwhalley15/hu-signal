using System.Text.Json.Serialization;

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("conversationId")]
    public Guid ConversationId { get; set; }

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = [];

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("think")]
    public bool Think { get; set; }

    [JsonPropertyName("options")]
    public ChatOptions Options { get; set; } = new();
}

public class ChatOptions
{
    [JsonPropertyName("contextSize")]
    public int ContextSize { get; set; }

    [JsonPropertyName("temperature")]
    public decimal Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public decimal TopP { get; set; }
}