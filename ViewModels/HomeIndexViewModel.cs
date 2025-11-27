namespace markly.ViewModels;

public class HomeIndexViewModel
{
    public IReadOnlyList<BookmarkListItemViewModel> Bookmarks { get; set; } = Array.Empty<BookmarkListItemViewModel>();
    public string ActiveFilter { get; set; } = "recent";
    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; } = 1;

    public bool HasBookmarks => Bookmarks.Count > 0;
    public bool ShowPagination => TotalPages > 1;
}

public class BookmarkListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int VoteCount { get; set; }
    public string? MediaImageUrl { get; set; }
    public string? MediaTextPreview { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}
