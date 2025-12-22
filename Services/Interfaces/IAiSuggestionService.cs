namespace markly.Services.Interfaces;

public class AiSuggestionResult
{
    public bool Success { get; set; }
    public List<string> SuggestedTags { get; set; } = new();
    public List<string> SuggestedCategories { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public interface IAiSuggestionService
{
    Task<AiSuggestionResult> GetSuggestionsAsync(string title, string? description);
}
