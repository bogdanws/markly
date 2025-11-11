namespace markly.Data.Entities;

/// <summary>
/// Many-to-many join table between Bookmark and Category
/// </summary>
public class BookmarkCategory
{
    public int BookmarkId { get; set; }
    public Bookmark Bookmark { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
