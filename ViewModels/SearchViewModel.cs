namespace markly.ViewModels;

public class SearchViewModel
{
    // Search criteria
    public string? Query { get; set; }
    public string? Tag { get; set; }
    public string Sort { get; set; } = "relevant";

    // Advanced filters
    public string? DateRange { get; set; } // "day", "week", "month", "year", or null for all time
    public string? Author { get; set; }
    public int? MinVotes { get; set; }
    public string? ContentType { get; set; } // "image", "text", or null for all

    // Results
    public IReadOnlyList<BookmarkListItemViewModel> Results { get; set; } = Array.Empty<BookmarkListItemViewModel>();

    // Pagination
    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalResults { get; set; }

    // Helper properties
    public bool HasResults => Results.Count > 0;
    public bool ShowPagination => TotalPages > 1;
    public bool HasSearchCriteria => !string.IsNullOrWhiteSpace(Query) || !string.IsNullOrWhiteSpace(Tag) ||
                                      !string.IsNullOrWhiteSpace(DateRange) || !string.IsNullOrWhiteSpace(Author) ||
                                      MinVotes.HasValue || !string.IsNullOrWhiteSpace(ContentType);
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(Tag) || !string.IsNullOrWhiteSpace(DateRange) ||
                                     !string.IsNullOrWhiteSpace(Author) || MinVotes.HasValue ||
                                     !string.IsNullOrWhiteSpace(ContentType);

    // Filter data
    public IEnumerable<string> PopularTags { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<string> Authors { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<TrendingSearchViewModel> TrendingSearches { get; set; } = Enumerable.Empty<TrendingSearchViewModel>();
}

public class TrendingSearchViewModel
{
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
}
