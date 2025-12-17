using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;
using markly.ViewModels.Admin;

namespace markly.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var totalBookmarks = await _context.Bookmarks.CountAsync();
        var totalComments = await _context.Comments.CountAsync();
        var totalCategories = await _context.Categories.CountAsync();
        var totalUsers = await _userManager.Users.CountAsync();

        var recentBookmarks = await _context.Bookmarks
            .Include(b => b.User)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .Select(b => new AdminBookmarkItemViewModel
            {
                Id = b.Id,
                Title = b.Title,
                AuthorName = b.User != null
                    ? (!string.IsNullOrEmpty(b.User.FirstName) ? $"{b.User.FirstName} {b.User.LastName}".Trim() : b.User.UserName ?? "Unknown")
                    : "Unknown",
                AuthorUserName = b.User != null ? b.User.UserName ?? "" : "",
                CreatedAt = b.CreatedAt,
                IsPublic = b.IsPublic
            })
            .ToListAsync();

        var recentComments = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Bookmark)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new AdminCommentItemViewModel
            {
                Id = c.Id,
                Content = c.Content.Length > 100 ? c.Content.Substring(0, 100) + "..." : c.Content,
                AuthorName = c.User != null
                    ? (!string.IsNullOrEmpty(c.User.FirstName) ? $"{c.User.FirstName} {c.User.LastName}".Trim() : c.User.UserName ?? "Unknown")
                    : "Unknown",
                AuthorUserName = c.User != null ? c.User.UserName ?? "" : "",
                BookmarkId = c.BookmarkId,
                BookmarkTitle = c.Bookmark != null ? c.Bookmark.Title : "Deleted Bookmark",
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            TotalBookmarks = totalBookmarks,
            TotalComments = totalComments,
            TotalCategories = totalCategories,
            TotalUsers = totalUsers,
            RecentBookmarks = recentBookmarks,
            RecentComments = recentComments
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Bookmarks(int page = 1, string? search = null)
    {
        const int pageSize = 20;

        var query = _context.Bookmarks
            .Include(b => b.User)
            .Include(b => b.Comments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b => b.Title.Contains(search) || b.Description.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var bookmarks = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new AdminBookmarkItemViewModel
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description.Length > 150 ? b.Description.Substring(0, 150) + "..." : b.Description,
                AuthorName = b.User != null
                    ? (!string.IsNullOrEmpty(b.User.FirstName) ? $"{b.User.FirstName} {b.User.LastName}".Trim() : b.User.UserName ?? "Unknown")
                    : "Unknown",
                AuthorUserName = b.User != null ? b.User.UserName ?? "" : "",
                CreatedAt = b.CreatedAt,
                IsPublic = b.IsPublic,
                CommentCount = b.Comments.Count
            })
            .ToListAsync();

        var model = new AdminBookmarksViewModel
        {
            Bookmarks = bookmarks,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            SearchQuery = search
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBookmark(int id)
    {
        var bookmark = await _context.Bookmarks.FindAsync(id);
        if (bookmark == null)
        {
            TempData["ErrorMessage"] = "Bookmark not found.";
            return RedirectToAction(nameof(Bookmarks));
        }

        var title = bookmark.Title;
        _context.Bookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminId} deleted bookmark {BookmarkId} ({BookmarkTitle})",
            _userManager.GetUserId(User), id, title);

        TempData["SuccessMessage"] = $"Bookmark \"{title}\" has been deleted.";
        return RedirectToAction(nameof(Bookmarks));
    }

    [HttpGet]
    public async Task<IActionResult> Comments(int page = 1, string? search = null)
    {
        const int pageSize = 20;

        var query = _context.Comments
            .Include(c => c.User)
            .Include(c => c.Bookmark)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Content.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminCommentItemViewModel
            {
                Id = c.Id,
                Content = c.Content,
                AuthorName = c.User != null
                    ? (!string.IsNullOrEmpty(c.User.FirstName) ? $"{c.User.FirstName} {c.User.LastName}".Trim() : c.User.UserName ?? "Unknown")
                    : "Unknown",
                AuthorUserName = c.User != null ? c.User.UserName ?? "" : "",
                BookmarkId = c.BookmarkId,
                BookmarkTitle = c.Bookmark != null ? c.Bookmark.Title : "Deleted Bookmark",
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        var model = new AdminCommentsViewModel
        {
            Comments = comments,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            SearchQuery = search
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment == null)
        {
            TempData["ErrorMessage"] = "Comment not found.";
            return RedirectToAction(nameof(Comments));
        }

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminId} deleted comment {CommentId}",
            _userManager.GetUserId(User), id);

        TempData["SuccessMessage"] = "Comment has been deleted.";
        return RedirectToAction(nameof(Comments));
    }

    [HttpGet]
    public async Task<IActionResult> Categories(int page = 1, string? search = null)
    {
        const int pageSize = 20;

        var query = _context.Categories
            .Include(c => c.User)
            .Include(c => c.BookmarkCategories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var categories = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminCategoryItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                OwnerName = c.User != null
                    ? (!string.IsNullOrEmpty(c.User.FirstName) ? $"{c.User.FirstName} {c.User.LastName}".Trim() : c.User.UserName ?? "Unknown")
                    : "Unknown",
                OwnerUserName = c.User != null ? c.User.UserName ?? "" : "",
                CreatedAt = c.CreatedAt,
                IsPublic = c.IsPublic,
                BookmarkCount = c.BookmarkCategories.Count
            })
            .ToListAsync();

        var model = new AdminCategoriesViewModel
        {
            Categories = categories,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            SearchQuery = search
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.BookmarkCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToAction(nameof(Categories));
        }

        var name = category.Name;

        // Remove bookmark-category associations first
        _context.BookmarkCategories.RemoveRange(category.BookmarkCategories);
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminId} deleted category {CategoryId} ({CategoryName})",
            _userManager.GetUserId(User), id, name);

        TempData["SuccessMessage"] = $"Category \"{name}\" has been deleted.";
        return RedirectToAction(nameof(Categories));
    }
}
