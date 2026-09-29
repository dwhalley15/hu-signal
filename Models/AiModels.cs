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

    public ClarityAiReportFacts Facts { get; set; } = new();

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

public class ClarityAiReportFacts
{
    public int DaysWithData { get; set; }

    public int TotalSessions { get; set; }

    public int BotSessions { get; set; }

    public decimal AveragePagesPerSession { get; set; }

    public decimal AverageScrollDepth { get; set; }

    public int DeadClicks { get; set; }

    public int RageClicks { get; set; }

    public int Quickbacks { get; set; }

    public int ScriptErrors { get; set; }

    public int ErrorClicks { get; set; }

    public int ExcessiveScrolls { get; set; }

    public int DirectReferrals { get; set; }

    public int GoogleReferrals { get; set; }
}