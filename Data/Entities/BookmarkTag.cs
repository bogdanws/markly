namespace markly.Data.Entities;

/// <summary>
/// Many-to-many join table between Bookmark and Tag
/// </summary>
public class BookmarkTag
{
    public int BookmarkId { get; set; }
    public Bookmark Bookmark { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}