using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

    [JsonIgnore]
    public string? VideoEmbedUrl => GetVideoEmbedUrl(VideoUrl);

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
    private static string? GetVideoEmbedUrl(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var url = source.Trim();

        var youtube = Regex.Match(url, @"(?:youtu\.be/|youtube\.com/(?:watch\?v=|embed/|v/))([A-Za-z0-9_-]{11})");
        if (youtube.Success)
        {
            return $"https://www.youtube.com/embed/{youtube.Groups[1].Value}";
        }

        var vimeo = Regex.Match(url, @"vimeo\.com/(?:.*/)?(\d+)");
        if (vimeo.Success)
        {
            return $"https://player.vimeo.com/video/{vimeo.Groups[1].Value}";
        }

        // Return null for unrecognized patterns to prevent XSS/clickjacking attacks
        return null;
    }
}
