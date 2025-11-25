using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using markly.Configuration;
using markly.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace markly.Services.Implementations;

public class AnthropicSuggestionService : IAiSuggestionService
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicSettings _settings;
    private readonly ILogger<AnthropicSuggestionService> _logger;
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";

    public AnthropicSuggestionService(
        HttpClient httpClient,
        IOptions<AnthropicSettings> settings,
        ILogger<AnthropicSuggestionService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AiSuggestionResult> GetSuggestionsAsync(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("Anthropic API key is not configured");
            return new AiSuggestionResult
            {
                Success = false,
                ErrorMessage = "AI suggestions are not available. API key not configured."
            };
        }

        try
        {
            var prompt = BuildPrompt(title, description);
            var response = await CallAnthropicApiAsync(prompt);
            return ParseResponse(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Anthropic API");
            return new AiSuggestionResult
            {
                Success = false,
                ErrorMessage = "Failed to connect to AI service. Please try again later."
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Anthropic API response");
            return new AiSuggestionResult
            {
                Success = false,
                ErrorMessage = "Failed to parse AI response. Please try again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Anthropic API");
            return new AiSuggestionResult
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred. Please try again."
            };
        }
    }

    private static string BuildPrompt(string title, string? description)
    {
        var contentParts = new List<string> { $"Title: {title}" };
        
        if (!string.IsNullOrWhiteSpace(description))
        {
            contentParts.Add($"Description: {description}");
        }

        return $$"""
            Analyze the following bookmark content and suggest relevant tags and categories.
            
            <bookmark>
            {{string.Join("\n", contentParts)}}
            </bookmark>

            Return your suggestions in the following JSON format only, with no additional text:
            {
                "tags": ["tag1", "tag2", "tag3"],
                "categories": ["category1", "category2"]
            }

            Guidelines:
            - Suggest 3-5 specific, lowercase tags that describe the content (e.g., "javascript", "tutorial", "web-development")
            - Suggest 2-3 broad category names that could organize this bookmark (e.g., "Programming", "Design", "News")
            - Tags should be single words or hyphenated phrases
            - Categories should be title-cased and can be multiple words
            - Be concise and relevant to the content
            """;
    }

    private async Task<string> CallAnthropicApiAsync(string prompt)
    {
        var request = new AnthropicRequest
        {
            Model = _settings.Model,
            MaxTokens = _settings.MaxTokens,
            Messages = new List<AnthropicMessage>
            {
                new() { Role = "user", Content = prompt }
            }
        };

        var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl);
        httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private AiSuggestionResult ParseResponse(string responseJson)
    {
        var response = JsonSerializer.Deserialize<AnthropicResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        if (response?.Content == null || response.Content.Count == 0)
        {
            return new AiSuggestionResult
            {
                Success = false,
                ErrorMessage = "Empty response from AI service."
            };
        }

        var textContent = response.Content.FirstOrDefault(c => c.Type == "text")?.Text;
        if (string.IsNullOrWhiteSpace(textContent))
        {
            return new AiSuggestionResult
            {
                Success = false,
                ErrorMessage = "No text content in AI response."
            };
        }

        // Extract JSON from the response (in case there's surrounding text)
        var jsonStart = textContent.IndexOf('{');
        var jsonEnd = textContent.LastIndexOf('}');
        
        if (jsonStart == -1 || jsonEnd == -1 || jsonEnd <= jsonStart)
        {
            _logger.LogWarning("Could not find JSON in AI response: {Response}", textContent);
            return new AiSuggestionResult
            {
                Success = false,
                ErrorMessage = "Invalid AI response format."
            };
        }

        var jsonPart = textContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
        var suggestions = JsonSerializer.Deserialize<SuggestionResponse>(jsonPart, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return new AiSuggestionResult
        {
            Success = true,
            SuggestedTags = suggestions?.Tags?.Take(5).ToList() ?? new List<string>(),
            SuggestedCategories = suggestions?.Categories?.Take(3).ToList() ?? new List<string>()
        };
    }

    #region Anthropic API Models

    private class AnthropicRequest
    {
        public string Model { get; set; } = string.Empty;
        public int MaxTokens { get; set; }
        public List<AnthropicMessage> Messages { get; set; } = new();
    }

    private class AnthropicMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class AnthropicResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<ContentBlock> Content { get; set; } = new();
    }

    private class ContentBlock
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    private class SuggestionResponse
    {
        public List<string> Tags { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }

    #endregion
}
