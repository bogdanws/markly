using System.Text.Json;
using System.Text.Json.Serialization;

namespace markly.Models;

public class BookmarkMediaContent
{
    public string? TextContent { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }

    [JsonIgnore]
    public bool HasAnyMedia =>
        !string.IsNullOrWhiteSpace(TextContent) ||
        !string.IsNullOrWhiteSpace(ImageUrl) ||
        !string.IsNullOrWhiteSpace(VideoUrl);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static BookmarkMediaContent FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new BookmarkMediaContent();
        }

        try
        {
            var media = JsonSerializer.Deserialize<BookmarkMediaContent>(json, SerializerOptions);
            return media ?? new BookmarkMediaContent();
        }
        catch
        {
            // Fallback: treat legacy content as plain text
            return new BookmarkMediaContent
            {
                TextContent = json
            };
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }
}
