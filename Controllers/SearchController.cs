using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public async Task<IActionResult> Index(string? q, int? categoryId, string? tag, string? sort, int page = 1)
    {
        var pageNumber = page < 1 ? 1 : page;
        var normalizedSort = NormalizeSort(sort);

        // Base query - only public bookmarks
        var bookmarksQuery = _context.Bookmarks
            .AsNoTracking()
            .Include(b => b.BookmarkCategories)
                .ThenInclude(bc => bc.Category)
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
                b.BookmarkCategories.Any(bc => EF.Functions.ILike(bc.Category.Name, searchPattern)) ||
                b.BookmarkTags.Any(bt => EF.Functions.ILike(bt.Tag.Name, searchPattern)));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            bookmarksQuery = bookmarksQuery.Where(b =>
                b.BookmarkCategories.Any(bc => bc.CategoryId == categoryId.Value));
        }

        // Filter by tag (partial match)
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagPattern = $"%{tag.Trim()}%";
            bookmarksQuery = bookmarksQuery.Where(b =>
                b.BookmarkTags.Any(bt => EF.Functions.ILike(bt.Tag.Name, tagPattern)));
        }

        // Get total count for pagination
        var totalCount = await bookmarksQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        // Project to include VoteCount
        var projectedQuery = bookmarksQuery
            .Select(b => new
            {
                Bookmark = b,
                User = b.User,
                VoteCount = b.Votes.Count
            });

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

        // Load categories for dropdown
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsPublic)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == categoryId
            })
            .ToListAsync();

        // Load popular tags for suggestions
        var popularTags = await _context.Tags
            .AsNoTracking()
            .Select(t => new { t.Name, Count = t.BookmarkTags.Count })
            .OrderByDescending(t => t.Count)
            .Take(20)
            .Select(t => t.Name)
            .ToListAsync();

        var model = new SearchViewModel
        {
            Query = q,
            CategoryId = categoryId,
            Tag = tag,
            Sort = normalizedSort,
            Results = bookmarkItems,
            PageNumber = pageNumber,
            TotalPages = totalPages,
            TotalResults = totalCount,
            Categories = categories,
            PopularTags = popularTags
        };

        return View(model);
    }

    private static string NormalizeSort(string? sort)
    {
        if (string.Equals(sort, "recent", StringComparison.OrdinalIgnoreCase))
        {
            return "recent";
        }

        return "relevant";
    }
}
