using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;
using markly.Helpers;
using markly.Models;
using markly.ViewModels;
using markly.Services.Interfaces;

namespace markly.Controllers;

public class BookmarksController : Controller
{
    private const int PageSize = 12;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BookmarksController> _logger;
    private readonly IAiSuggestionService _aiSuggestionService;
    private readonly IRateLimitingService _rateLimitingService;

    public BookmarksController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<BookmarksController> logger,
        IAiSuggestionService aiSuggestionService,
        IRateLimitingService rateLimitingService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _aiSuggestionService = aiSuggestionService;
        _rateLimitingService = rateLimitingService;
    }

    #region Browse

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Browse(string? sort, int? tag, int? category, string? time, int page = 1)
    {
        var model = await GetBrowseDataAsync(sort, tag, category, time, page);
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> BrowseFeed(string? sort, int? tag, int? category, string? time, int page = 1)
    {
        var model = await GetBrowseDataAsync(sort, tag, category, time, page);
        return PartialView("_BrowseFeed", model);
    }

    private async Task<BrowseViewModel> GetBrowseDataAsync(string? sort, int? tagId, int? categoryId, string? timeRange, int page)
    {
        var normalizedSort = NormalizeSort(sort);
        var pageNumber = page < 1 ? 1 : page;
        var currentUserId = _userManager.GetUserId(User);

        // Get trending tags (top 8 by bookmark count)
        var trendingTags = await _context.Tags
            .AsNoTracking()
            .Select(t => new TrendingTagViewModel
            {
                Id = t.Id,
                Name = t.Name,
                BookmarkCount = t.BookmarkTags.Count(bt => bt.Bookmark.IsPublic)
            })
            .Where(t => t.BookmarkCount > 0)
            .OrderByDescending(t => t.BookmarkCount)
            .Take(8)
            .ToListAsync();

        // Get public categories with bookmark counts (top 6)
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsPublic)
            .Select(c => new BrowseCategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                BookmarkCount = c.BookmarkCategories.Count(bc => bc.Bookmark.IsPublic),
                OwnerName = c.User.FirstName != null ? c.User.FirstName : c.User.UserName ?? "Unknown"
            })
            .Where(c => c.BookmarkCount > 0)
            .OrderByDescending(c => c.BookmarkCount)
            .Take(6)
            .ToListAsync();

        // Build bookmarks query
        var bookmarksQuery = _context.Bookmarks
            .AsNoTracking()
            .Where(b => b.IsPublic);

        // Apply tag filter
        if (tagId.HasValue)
        {
            bookmarksQuery = bookmarksQuery.Where(b => b.BookmarkTags.Any(bt => bt.TagId == tagId.Value));
        }

        // Apply category filter
        if (categoryId.HasValue)
        {
            bookmarksQuery = bookmarksQuery.Where(b => b.BookmarkCategories.Any(bc => bc.CategoryId == categoryId.Value));
        }

        // Apply time range filter
        if (!string.IsNullOrWhiteSpace(timeRange))
        {
            var cutoffDate = timeRange.ToLowerInvariant() switch
            {
                "day" => DateTime.UtcNow.AddDays(-1),
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddMonths(-1),
                "year" => DateTime.UtcNow.AddYears(-1),
                _ => (DateTime?)null
            };

            if (cutoffDate.HasValue)
            {
                bookmarksQuery = bookmarksQuery.Where(b => b.CreatedAt >= cutoffDate.Value);
            }
        }

        var totalCount = await bookmarksQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        var projectedQuery = bookmarksQuery
            .Select(b => new
            {
                Bookmark = b,
                User = b.User,
                VoteCount = b.Votes.Count,
                CommentCount = b.Comments.Count,
                IsLikedByCurrentUser = currentUserId != null && b.Votes.Any(v => v.UserId == currentUserId)
            });

        // Apply sorting
        projectedQuery = normalizedSort switch
        {
            "popular" => projectedQuery.OrderByDescending(x => x.VoteCount).ThenByDescending(x => x.Bookmark.CreatedAt),
            "discussed" => projectedQuery.OrderByDescending(x => x.CommentCount).ThenByDescending(x => x.Bookmark.CreatedAt),
            _ => projectedQuery.OrderByDescending(x => x.Bookmark.CreatedAt)
        };

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

        // Get active filter names
        string? activeTagName = null;
        string? activeCategoryName = null;

        if (tagId.HasValue)
        {
            activeTagName = await _context.Tags
                .Where(t => t.Id == tagId.Value)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();
        }

        if (categoryId.HasValue)
        {
            activeCategoryName = await _context.Categories
                .Where(c => c.Id == categoryId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

        return new BrowseViewModel
        {
            Sort = normalizedSort,
            TagId = tagId,
            CategoryId = categoryId,
            TimeRange = timeRange,
            TrendingTags = trendingTags,
            Categories = categories,
            Bookmarks = bookmarkItems,
            PageNumber = pageNumber,
            TotalPages = totalPages,
            TotalResults = totalCount,
            ActiveTagName = activeTagName,
            ActiveCategoryName = activeCategoryName
        };
    }

    private static string NormalizeSort(string? sort)
    {
        return sort?.ToLowerInvariant() switch
        {
            "popular" => "popular",
            "discussed" => "discussed",
            _ => "recent"
        };
    }

    #endregion

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var model = new BookmarkFormViewModel();
        await LoadCategories(model, user.Id);
        await LoadTags(model);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookmarkFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            await LoadCategories(model, user.Id);
            await LoadTags(model);
            return View(model);
        }

        var bookmark = new Bookmark
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            IsPublic = model.IsPublic,
            Content = BuildMediaContent(model).ToJson(),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync(); // Save to get ID

        // Add Categories
        await UpdateBookmarkCategories(bookmark, model.SelectedCategoryIds, user.Id);

        // Add Tags
        await UpdateBookmarkTags(bookmark, model.SelectedTagIds);

        TempData["SuccessMessage"] = "Bookmark created successfully.";
        return RedirectToAction(nameof(Details), new { id = bookmark.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var bookmark = await _context.Bookmarks
            .Include(b => b.BookmarkCategories)
            .Include(b => b.BookmarkTags)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bookmark == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!IsOwner(bookmark, currentUserId))
        {
            return Forbid();
        }

        var media = BookmarkMediaContent.FromJson(bookmark.Content);
        var model = BuildFormViewModel(bookmark, media);

        // Load categories and tags
        await LoadCategories(model, currentUserId!);
        await LoadTags(model);

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookmarkFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            await LoadCategories(model, user.Id);
            await LoadTags(model);
            return View(model);
        }

        var bookmark = await _context.Bookmarks
            .Include(b => b.BookmarkCategories)
            .Include(b => b.BookmarkTags)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bookmark == null)
        {
            return NotFound();
        }

        if (!IsOwner(bookmark, user.Id))
        {
            return Forbid();
        }

        bookmark.Title = model.Title.Trim();
        bookmark.Description = model.Description.Trim();
        bookmark.IsPublic = model.IsPublic;
        bookmark.Content = BuildMediaContent(model).ToJson();
        bookmark.UpdatedAt = DateTime.UtcNow;

        // Update Categories
        _context.BookmarkCategories.RemoveRange(bookmark.BookmarkCategories);
        await UpdateBookmarkCategories(bookmark, model.SelectedCategoryIds, user.Id);

        // Update Tags
        _context.BookmarkTags.RemoveRange(bookmark.BookmarkTags);
        await UpdateBookmarkTags(bookmark, model.SelectedTagIds);

        TempData["SuccessMessage"] = "Bookmark updated successfully.";
        return RedirectToAction(nameof(Details), new { id = bookmark.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var bookmark = await _context.Bookmarks
            .Include(b => b.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bookmark == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!IsOwner(bookmark, currentUserId))
        {
            return Forbid();
        }

        var model = BuildDetailsViewModel(bookmark, currentUserId);
        return View(model);
    }

    [Authorize]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var bookmark = await _context.Bookmarks.FirstOrDefaultAsync(b => b.Id == id);
        if (bookmark == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!IsOwner(bookmark, currentUserId))
        {
            return Forbid();
        }

        _context.Bookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Bookmark deleted successfully.";
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var bookmark = await _context.Bookmarks
            .Include(b => b.User)
            .Include(b => b.Comments)
                .ThenInclude(c => c.User)
            .Include(b => b.Votes)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bookmark == null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!bookmark.IsPublic && !IsOwner(bookmark, currentUserId))
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        ViewData["CurrentUserProfilePictureUrl"] = currentUser?.ProfilePictureUrl;

        var model = BuildDetailsViewModel(bookmark, currentUserId);
        return View(model);
    }

    private async Task LoadCategories(BookmarkFormViewModel model, string userId)
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        model.AvailableCategories = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = model.SelectedCategoryIds.Contains(c.Id)
        }).ToList();
    }

    private async Task LoadTags(BookmarkFormViewModel model)
    {
        var tags = await _context.Tags
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        model.AvailableTags = tags.Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = t.Name,
            Selected = model.SelectedTagIds.Contains(t.Id)
        }).ToList();
    }

    private static bool IsOwner(Bookmark bookmark, string? userId)
    {
        return !string.IsNullOrEmpty(userId) && bookmark.UserId == userId;
    }

    private static BookmarkMediaContent BuildMediaContent(BookmarkFormViewModel model)
    {
        var videoUrl = string.IsNullOrWhiteSpace(model.VideoUrl) ? null : model.VideoUrl.Trim();
        var imageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();

        // Auto-apply video thumbnail if no image is set
        if (imageUrl == null && videoUrl != null)
        {
            imageUrl = BookmarkMediaContent.GetVideoThumbnailUrl(videoUrl);
        }

        return new BookmarkMediaContent
        {
            TextContent = string.IsNullOrWhiteSpace(model.TextContent) ? null : model.TextContent.Trim(),
            ImageUrl = imageUrl,
            VideoUrl = videoUrl
        };
    }

    private static BookmarkFormViewModel BuildFormViewModel(Bookmark bookmark, BookmarkMediaContent media)
    {
        return new BookmarkFormViewModel
        {
            Id = bookmark.Id,
            Title = bookmark.Title,
            Description = bookmark.Description,
            IsPublic = bookmark.IsPublic,
            TextContent = media.TextContent,
            ImageUrl = media.ImageUrl,
            VideoUrl = media.VideoUrl,
            SelectedCategoryIds = bookmark.BookmarkCategories.Select(bc => bc.CategoryId).ToList(),
            SelectedTagIds = bookmark.BookmarkTags.Select(bt => bt.TagId).ToList()
        };
    }

    private static BookmarkDetailsViewModel BuildDetailsViewModel(Bookmark bookmark, string? currentUserId)
    {
        var media = BookmarkMediaContent.FromJson(bookmark.Content);
        return new BookmarkDetailsViewModel
        {
            Id = bookmark.Id,
            Title = bookmark.Title,
            Description = bookmark.Description,
            CreatedAt = bookmark.CreatedAt,
            UpdatedAt = bookmark.UpdatedAt,
            IsPublic = bookmark.IsPublic,
            AuthorName = UserHelper.GetAuthorName(bookmark.User),
            AuthorUsername = bookmark.User?.UserName ?? string.Empty,
            CanEdit = IsOwner(bookmark, currentUserId),
            MediaContent = media,
            VoteCount = bookmark.Votes?.Count ?? 0,
            IsLikedByCurrentUser = !string.IsNullOrEmpty(currentUserId) && (bookmark.Votes?.Any(v => v.UserId == currentUserId) ?? false),
            Comments = bookmark.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorName = UserHelper.GetAuthorName(c.User),
                    AuthorFirstName = UserHelper.GetAuthorFirstName(c.User),
                    AuthorUserName = c.User != null ? c.User.UserName ?? string.Empty : string.Empty,
                    AuthorProfilePictureUrl = c.User?.ProfilePictureUrl,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsOwner = !string.IsNullOrEmpty(currentUserId) && c.UserId == currentUserId
                })
                .ToList()
        };
    }

    private async Task UpdateBookmarkCategories(Bookmark bookmark, List<int> categoryIds, string userId)
    {
        if (!categoryIds.Any())
        {
            await _context.SaveChangesAsync();
            return;
        }

        var validCategoryIds = await _context.Categories
            .Where(c => c.UserId == userId && categoryIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var catId in validCategoryIds)
        {
            _context.BookmarkCategories.Add(new BookmarkCategory
            {
                BookmarkId = bookmark.Id,
                CategoryId = catId
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task UpdateBookmarkTags(Bookmark bookmark, List<int> tagIds)
    {
        if (!tagIds.Any())
        {
            return;
        }

        var validTagIds = await _context.Tags
            .Where(t => tagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        foreach (var tagId in validTagIds)
        {
            _context.BookmarkTags.Add(new BookmarkTag
            {
                BookmarkId = bookmark.Id,
                TagId = tagId
            });
        }

        await _context.SaveChangesAsync();
    }

    #region AI Suggestions

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuggestTags([FromBody] SuggestTagsRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(new { success = false, error = "User not authenticated." });
        }

        // Rate limiting check
        var canProceed = await _rateLimitingService.TryAcquireAsync(user.Id, "SuggestTags");
        if (!canProceed)
        {
            var waitTime = await _rateLimitingService.GetTimeUntilNextAllowedAsync(user.Id, "SuggestTags");
            var waitSeconds = waitTime?.TotalSeconds ?? 60;
            return StatusCode(429, new
            {
                success = false,
                error = $"Rate limit exceeded. Please wait {Math.Ceiling(waitSeconds)} seconds before trying again."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { success = false, error = "Title is required for suggestions." });
        }

        // Limit description length to prevent excessive API token usage
        var description = request.Description?.Length > 500
            ? request.Description[..500]
            : request.Description;

        var result = await _aiSuggestionService.GetSuggestionsAsync(request.Title, description);

        if (!result.Success)
        {
            return StatusCode(500, new { success = false, error = result.ErrorMessage });
        }

        // Look up existing tags and categories that match suggestions
        var existingTags = await _context.Tags
            .Where(t => result.SuggestedTags.Select(s => s.ToLower()).Contains(t.Name.ToLower()))
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        var existingCategories = await _context.Categories
            .Where(c => c.UserId == user.Id && result.SuggestedCategories.Select(s => s.ToLower()).Contains(c.Name.ToLower()))
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            suggestedTags = result.SuggestedTags.Select(tagName => new
            {
                name = tagName,
                id = existingTags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase))?.Id,
                exists = existingTags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase))
            }),
            suggestedCategories = result.SuggestedCategories.Select(catName => new
            {
                name = catName,
                id = existingCategories.FirstOrDefault(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase))?.Id,
                exists = existingCategories.Any(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase))
            })
        });
    }

    public class SuggestTagsRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    #endregion
}
