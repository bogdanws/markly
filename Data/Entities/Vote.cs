namespace markly.Data.Entities;

public class Vote
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // foreign keys
    public int BookmarkId { get; set; }
    public Bookmark Bookmark { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}