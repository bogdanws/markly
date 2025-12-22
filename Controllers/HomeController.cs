using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? filter, int page = 1)
    {
        var model = await GetBookmarksAsync(filter, page);
        ViewData["WelcomeName"] = await GetWelcomeNameAsync();
        return View(model);
    }

    public async Task<IActionResult> Feed(string? filter, int page = 1)
    {
        var model = await GetBookmarksAsync(filter, page);
        return PartialView("_BookmarkFeed", model);
    }

    private async Task<HomeIndexViewModel> GetBookmarksAsync(string? filter, int page)
    {
        var normalizedFilter = NormalizeFilter(filter);
        var pageNumber = page < 1 ? 1 : page;
        var currentUserId = _userManager.GetUserId(User);

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
                VoteCount = b.Votes.Count,
                IsLikedByCurrentUser = currentUserId != null && b.Votes.Any(v => v.UserId == currentUserId)
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
                MediaTextPreview = UserHelper.BuildTextPreview(media.TextContent, 160),
                IsLikedByCurrentUser = item.IsLikedByCurrentUser
            };
        }).ToList();

        return new HomeIndexViewModel
        {
            ActiveFilter = normalizedFilter,
            Bookmarks = bookmarkItems,
            PageNumber = pageNumber,
            TotalPages = totalPages
        };
    }

    private async Task<string?> GetWelcomeNameAsync()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return null;
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return null;
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return user.FirstName ?? user.LastName ?? user.UserName;
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
}
