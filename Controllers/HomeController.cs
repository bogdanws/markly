using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;
using markly.Helpers;
using markly.Models;
using markly.ViewModels;

namespace markly.Controllers;

public class HomeController : Controller
{
    private const int PageSize = 10;

    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index(string? filter, int page = 1)
    {
        var normalizedFilter = NormalizeFilter(filter);
        var pageNumber = page < 1 ? 1 : page;

        var bookmarksQuery = _context.Bookmarks
            .AsNoTracking()
            .Where(b => b.IsPublic);

        var totalCount = await bookmarksQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        var projectedQuery = bookmarksQuery
            .Select(b => new
            {
                Bookmark = b,
                User = b.User,
                VoteCount = b.Votes.Count
            });

        projectedQuery = normalizedFilter == "popular"
            ? projectedQuery.OrderByDescending(x => x.VoteCount).ThenByDescending(x => x.Bookmark.CreatedAt)
            : projectedQuery.OrderByDescending(x => x.Bookmark.CreatedAt);

        var bookmarks = await projectedQuery
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

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
                MediaTextPreview = BuildTextPreview(media.TextContent)
            };
        }).ToList();

        var model = new HomeIndexViewModel
        {
            ActiveFilter = normalizedFilter,
            Bookmarks = bookmarkItems,
            PageNumber = pageNumber,
            TotalPages = totalPages
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static string NormalizeFilter(string? filter)
    {
        if (string.Equals(filter, "popular", StringComparison.OrdinalIgnoreCase))
        {
            return "popular";
        }

        return "recent";
    }

    private static string? BuildTextPreview(string? textContent)
    {
        if (string.IsNullOrWhiteSpace(textContent))
        {
            return null;
        }

        var trimmed = textContent.Trim();
        return trimmed.Length <= 160 ? trimmed : $"{trimmed.Substring(0, 160)}...";
    }
}
