using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;
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
    public IActionResult Create()
    {
        return View(new BookmarkFormViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookmarkFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
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
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Bookmark created successfully.";
        return RedirectToAction(nameof(Details), new { id = bookmark.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var bookmark = await _context.Bookmarks
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

        if (!ModelState.IsValid)
        {
            return View(model);
        }

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

        bookmark.Title = model.Title.Trim();
        bookmark.Description = model.Description.Trim();
        bookmark.IsPublic = model.IsPublic;
        bookmark.Content = BuildMediaContent(model).ToJson();
        bookmark.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

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
            VideoUrl = media.VideoUrl
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
            AuthorName = GetAuthorName(bookmark.User),
            CanEdit = IsOwner(bookmark, currentUserId),
            MediaContent = media
        };
    }

    private static string GetAuthorName(ApplicationUser? user)
    {
        if (user == null)
        {
            return "Unknown";
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return user.UserName ?? "Unknown";
    }
}
