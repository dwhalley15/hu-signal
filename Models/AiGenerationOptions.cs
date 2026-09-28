public class AiGenerationOptions
{
    public const string SectionName = "HuSignal:AI";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 120;

    public decimal Temperature { get; set; } = 0.3m;

    public decimal TopP { get; set; } = 0.9m;

    public int ContextSize { get; set; } = 16000;
}