using Microsoft.AspNetCore.Mvc.Rendering;

namespace markly.ViewModels;

public class SearchViewModel
{
    // Search criteria
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public string? Tag { get; set; }
    public string Sort { get; set; } = "relevant";

    // Results
    public IReadOnlyList<BookmarkListItemViewModel> Results { get; set; } = Array.Empty<BookmarkListItemViewModel>();

    // Pagination
    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalResults { get; set; }

    // Helper properties
    public bool HasResults => Results.Count > 0;
    public bool ShowPagination => TotalPages > 1;
    public bool HasSearchCriteria => !string.IsNullOrWhiteSpace(Query) || CategoryId.HasValue || !string.IsNullOrWhiteSpace(Tag);

    // Filter data for dropdowns
    public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<string> PopularTags { get; set; } = Enumerable.Empty<string>();
}
