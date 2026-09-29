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

public class ClarityAiReport
{
    public string Title { get; set; } = string.Empty;

    public string ExecutiveSummary { get; set; } = string.Empty;

    public string TrafficAndEngagement { get; set; } = string.Empty;

    public string UserBehaviourIssues { get; set; } = string.Empty;

    public string PopularContent { get; set; } = string.Empty;

    public string AudienceAndTechnology { get; set; } = string.Empty;

    public string Referrals { get; set; } = string.Empty;

    public List<ClarityAiRecommendation> Recommendations { get; set; } = [];

    public string Limitations { get; set; } = string.Empty;
}

public class ClarityAiRecommendation
{
    public string Priority { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool RequiresFurtherInvestigation { get; set; }
}