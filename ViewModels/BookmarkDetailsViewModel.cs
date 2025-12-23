using markly.Models;

namespace markly.ViewModels;

public class BookmarkDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
    public BookmarkMediaContent MediaContent { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
    public int VoteCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}
