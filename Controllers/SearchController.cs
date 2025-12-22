using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Helpers;
using markly.Models;
using markly.ViewModels;

namespace markly.Controllers;

public class SearchController : Controller
{
    private const int PageSize = 10;

    private readonly ILogger<SearchController> _logger;
    private readonly ApplicationDbContext _context;

    public SearchController(ILogger<SearchController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? q,
        string? tag,
        string? sort,
        string? dateRange,
        string? author,
        int? minVotes,
        string? contentType,
        int page = 1)
    {
        var model = await BuildSearchViewModel(q, tag, sort, dateRange, author, minVotes, contentType, page);
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Results(
        string? q,
        string? tag,
        string? sort,
        string? dateRange,
        string? author,
        int? minVotes,
        string? contentType,
        int page = 1)
    {
        var model = await BuildSearchViewModel(q, tag, sort, dateRange, author, minVotes, contentType, page);
        return PartialView("_SearchResults", model);
    }

    private async Task<SearchViewModel> BuildSearchViewModel(
        string? q,
        string? tag,
        string? sort,
        string? dateRange,
        string? author,
        int? minVotes,
        string? contentType,
        int page)
    {
        var pageNumber = page < 1 ? 1 : page;
        var normalizedSort = NormalizeSort(sort);
        var normalizedDateRange = NormalizeDateRange(dateRange);
        var normalizedContentType = NormalizeContentType(contentType);

        // Base query - only public bookmarks
        var bookmarksQuery = _context.Bookmarks
            .AsNoTracking()
            .Include(b => b.BookmarkTags)
                .ThenInclude(bt => bt.Tag)
            .Where(b => b.IsPublic);

        // Apply search filters
        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchPattern = $"%{q.Trim()}%";
            bookmarksQuery = bookmarksQuery.Where(b =>
                EF.Functions.ILike(b.Title, searchPattern) ||
                EF.Functions.ILike(b.Description, searchPattern) ||
                b.BookmarkTags.Any(bt => EF.Functions.ILike(bt.Tag.Name, searchPattern)));
        }

        // Filter by tag (partial match)
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagPattern = $"%{tag.Trim()}%";
            bookmarksQuery = bookmarksQuery.Where(b =>
                b.BookmarkTags.Any(bt => EF.Functions.ILike(bt.Tag.Name, tagPattern)));
        }

        // Filter by date range
        if (!string.IsNullOrWhiteSpace(normalizedDateRange))
        {
            var cutoffDate = normalizedDateRange switch
            {
                "day" => DateTime.UtcNow.AddDays(-1),
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddMonths(-1),
                "year" => DateTime.UtcNow.AddYears(-1),
                _ => DateTime.MinValue
            };

            if (cutoffDate != DateTime.MinValue)
            {
                bookmarksQuery = bookmarksQuery.Where(b => b.CreatedAt >= cutoffDate);
            }
        }

        // Filter by author (search in FirstName, LastName, full name, or UserName)
        if (!string.IsNullOrWhiteSpace(author))
        {
            var authorPattern = $"%{author.Trim()}%";
            bookmarksQuery = bookmarksQuery.Where(b =>
                (b.User.FirstName != null && EF.Functions.ILike(b.User.FirstName, authorPattern)) ||
                (b.User.LastName != null && EF.Functions.ILike(b.User.LastName, authorPattern)) ||
                EF.Functions.ILike((b.User.FirstName ?? "") + " " + (b.User.LastName ?? ""), authorPattern) ||
                (b.User.UserName != null && EF.Functions.ILike(b.User.UserName, authorPattern)));
        }

        // Filter by content type
        if (!string.IsNullOrWhiteSpace(normalizedContentType))
        {
            if (normalizedContentType == "image")
            {
                bookmarksQuery = bookmarksQuery.Where(b => b.Content != null && b.Content.Contains("\"imageUrl\""));
            }
            else if (normalizedContentType == "text")
            {
                bookmarksQuery = bookmarksQuery.Where(b => b.Content != null && b.Content.Contains("\"textContent\""));
            }
        }

        // Project to include VoteCount
        var projectedQuery = bookmarksQuery
            .Select(b => new
            {
                Bookmark = b,
                User = b.User,
                VoteCount = b.Votes.Count
            });

        // Filter by minimum votes (after projection)
        if (minVotes.HasValue && minVotes.Value > 0)
        {
            projectedQuery = projectedQuery.Where(x => x.VoteCount >= minVotes.Value);
        }

        // Get total count for pagination
        var totalCount = await projectedQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        // Apply ordering - "relevant" orders by VoteCount then CreatedAt, "recent" by CreatedAt only
        projectedQuery = normalizedSort == "relevant"
            ? projectedQuery.OrderByDescending(x => x.VoteCount).ThenByDescending(x => x.Bookmark.CreatedAt)
            : projectedQuery.OrderByDescending(x => x.Bookmark.CreatedAt);

        // Apply pagination
        var bookmarks = await projectedQuery
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Map to view models
        var bookmarkItems = bookmarks.Select(item =>
        {
            var b = item.Bookmark;
            var media = BookmarkMediaContent.FromJson(b.Content);
            return new BookmarkListItemViewModel
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                AuthorName = UserHelper.GetAuthorName(item.User),
                CreatedAt = b.CreatedAt,
                VoteCount = item.VoteCount,
                MediaImageUrl = media.ImageUrl,
                MediaTextPreview = UserHelper.BuildTextPreview(media.TextContent, 160)
            };
        }).ToList();

        // Load popular tags for suggestions
        var popularTags = await _context.Tags
            .AsNoTracking()
            .Select(t => new { t.Name, Count = t.BookmarkTags.Count })
            .OrderByDescending(t => t.Count)
            .Take(20)
            .Select(t => t.Name)
            .ToListAsync();

        // Load authors for filter dropdown (use full name or username)
        var authors = await _context.Users
            .AsNoTracking()
            .Where(u => u.Bookmarks.Any(b => b.IsPublic))
            .Select(u => (u.FirstName != null && u.LastName != null)
                ? (u.FirstName + " " + u.LastName).Trim()
                : (u.FirstName ?? u.LastName ?? u.UserName ?? "Unknown"))
            .Where(name => name != "Unknown" && name != "")
            .Distinct()
            .OrderBy(a => a)
            .Take(50)
            .ToListAsync();

        // Generate trending searches (based on popular tags)
        var trendingSearches = popularTags.Take(5).Select(t => new TrendingSearchViewModel
        {
            Query = t,
            ResultCount = _context.BookmarkTags.Count(bt => bt.Tag.Name == t && bt.Bookmark.IsPublic)
        }).ToList();

        return new SearchViewModel
        {
            Query = q,
            Tag = tag,
            Sort = normalizedSort,
            DateRange = normalizedDateRange,
            Author = author,
            MinVotes = minVotes,
            ContentType = normalizedContentType,
            Results = bookmarkItems,
            PageNumber = pageNumber,
            TotalPages = totalPages,
            TotalResults = totalCount,
            PopularTags = popularTags,
            Authors = authors,
            TrendingSearches = trendingSearches
        };
    }

    private static string NormalizeSort(string? sort)
    {
        if (string.Equals(sort, "recent", StringComparison.OrdinalIgnoreCase))
        {
            return "recent";
        }

        return "relevant";
    }

    private static string? NormalizeDateRange(string? dateRange)
    {
        return dateRange?.ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            "month" => "month",
            "year" => "year",
            _ => null
        };
    }

    private static string? NormalizeContentType(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image" => "image",
            "text" => "text",
            _ => null
        };
    }
}
