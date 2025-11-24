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

namespace markly.Controllers;

public class BookmarksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BookmarksController> _logger;

    public BookmarksController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<BookmarksController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var model = new BookmarkFormViewModel();
        await LoadCategories(model, user.Id);
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

        TempData["SuccessMessage"] = "Bookmark created successfully.";
        return RedirectToAction(nameof(Details), new { id = bookmark.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var bookmark = await _context.Bookmarks
            .Include(b => b.BookmarkCategories)
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
        
        // Load categories
        await LoadCategories(model, currentUserId!);

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
            return View(model);
        }

        var bookmark = await _context.Bookmarks
            .Include(b => b.BookmarkCategories)
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

    private static bool IsOwner(Bookmark bookmark, string? userId)
    {
        return !string.IsNullOrEmpty(userId) && bookmark.UserId == userId;
    }

    private static BookmarkMediaContent BuildMediaContent(BookmarkFormViewModel model)
    {
        return new BookmarkMediaContent
        {
            TextContent = string.IsNullOrWhiteSpace(model.TextContent) ? null : model.TextContent.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim(),
            VideoUrl = string.IsNullOrWhiteSpace(model.VideoUrl) ? null : model.VideoUrl.Trim()
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
            SelectedCategoryIds = bookmark.BookmarkCategories.Select(bc => bc.CategoryId).ToList()
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
            CanEdit = IsOwner(bookmark, currentUserId),
            MediaContent = media,
            Comments = bookmark.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorName = UserHelper.GetAuthorName(c.User),
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
}
