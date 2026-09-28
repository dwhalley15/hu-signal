public class ChatMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

public class ChatCompletionRequest
{
    public string Model { get; set; } = string.Empty;

    public bool Stream { get; set; }

    public Guid ConversationId { get; set; }

    public List<ChatMessage> Messages { get; set; } = [];

    public ChatOptions Options { get; set; } = new();
}

public class ChatOptions
{
    public int ContextSize { get; set; }

    public decimal Temperature { get; set; }

    public decimal TopP { get; set; }
}