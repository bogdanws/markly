namespace markly.Data.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // foreign key
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    // navigation properties
    public ICollection<BookmarkCategory> BookmarkCategories { get; set; } = new List<BookmarkCategory>();
}
