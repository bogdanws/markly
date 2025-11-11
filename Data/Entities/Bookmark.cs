namespace markly.Data.Entities;

public class Bookmark
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Content { get; set; } // Text, images, video embeds
    public bool IsPublic { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // foreign key
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<BookmarkTag> BookmarkTags { get; set; } = new List<BookmarkTag>();
    public ICollection<BookmarkCategory> BookmarkCategories { get; set; } = new List<BookmarkCategory>();

    // computed property
    public int VoteCount => Votes?.Count ?? 0;
}