namespace markly.Configuration;

public class AnthropicSettings
{
    public const string SectionName = "Anthropic";
    
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-4-5-haiku-20250514";
    public int MaxTokens { get; set; } = 256;
}
