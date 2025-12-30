namespace markly.ViewModels;

public class BrowseViewModel
{
    // Filters
    public string Sort { get; set; } = "recent";
    public int? TagId { get; set; }
    public int? CategoryId { get; set; }
    public string? TimeRange { get; set; } // "day", "week", "month", "year", or null for all time

    // Discovery sections
    public IReadOnlyList<TrendingTagViewModel> TrendingTags { get; set; } = Array.Empty<TrendingTagViewModel>();
    public IReadOnlyList<BrowseCategoryViewModel> Categories { get; set; } = Array.Empty<BrowseCategoryViewModel>();

    // Results
    public IReadOnlyList<BookmarkListItemViewModel> Bookmarks { get; set; } = Array.Empty<BookmarkListItemViewModel>();

    // Pagination
    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalResults { get; set; }

    // Active filter info (for displaying chips)
    public string? ActiveTagName { get; set; }
    public string? ActiveCategoryName { get; set; }

    // Helper properties
    public bool HasBookmarks => Bookmarks.Count > 0;
    public bool ShowPagination => TotalPages > 1;
    public bool HasActiveFilters => TagId.HasValue || CategoryId.HasValue || !string.IsNullOrWhiteSpace(TimeRange);
}

public class TrendingTagViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BookmarkCount { get; set; }
}

public class BrowseCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BookmarkCount { get; set; }
    public string OwnerName { get; set; } = string.Empty;
}
