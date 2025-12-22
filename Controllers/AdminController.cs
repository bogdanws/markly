using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;
using markly.Helpers;
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

        var recentBookmarksRaw = await _context.Bookmarks
            .Include(b => b.User)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentBookmarks = recentBookmarksRaw.Select(b => new AdminBookmarkItemViewModel
        {
            Id = b.Id,
            Title = b.Title,
            AuthorName = UserHelper.GetAuthorName(b.User),
            AuthorUserName = b.User?.UserName ?? "",
            CreatedAt = b.CreatedAt,
            IsPublic = b.IsPublic
        }).ToList();

        var recentCommentsRaw = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Bookmark)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentComments = recentCommentsRaw.Select(c => new AdminCommentItemViewModel
        {
            Id = c.Id,
            Content = c.Content.Length > 100 ? c.Content.Substring(0, 100) + "..." : c.Content,
            AuthorName = UserHelper.GetAuthorName(c.User),
            AuthorUserName = c.User?.UserName ?? "",
            BookmarkId = c.BookmarkId,
            BookmarkTitle = c.Bookmark?.Title ?? "Deleted Bookmark",
            CreatedAt = c.CreatedAt
        }).ToList();

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
            var pattern = $"%{search}%";
            query = query.Where(b => EF.Functions.ILike(b.Title, pattern) || EF.Functions.ILike(b.Description, pattern));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var bookmarksRaw = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var bookmarks = bookmarksRaw.Select(b => new AdminBookmarkItemViewModel
        {
            Id = b.Id,
            Title = b.Title,
            Description = b.Description.Length > 150 ? b.Description.Substring(0, 150) + "..." : b.Description,
            AuthorName = UserHelper.GetAuthorName(b.User),
            AuthorUserName = b.User?.UserName ?? "",
            CreatedAt = b.CreatedAt,
            IsPublic = b.IsPublic,
            CommentCount = b.Comments.Count
        }).ToList();

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
            query = query.Where(c => EF.Functions.ILike(c.Content, $"%{search}%"));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var commentsRaw = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var comments = commentsRaw.Select(c => new AdminCommentItemViewModel
        {
            Id = c.Id,
            Content = c.Content,
            AuthorName = UserHelper.GetAuthorName(c.User),
            AuthorUserName = c.User?.UserName ?? "",
            BookmarkId = c.BookmarkId,
            BookmarkTitle = c.Bookmark?.Title ?? "Deleted Bookmark",
            CreatedAt = c.CreatedAt
        }).ToList();

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
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{search}%"));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var categoriesRaw = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = categoriesRaw.Select(c => new AdminCategoryItemViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            OwnerName = UserHelper.GetAuthorName(c.User),
            OwnerUserName = c.User?.UserName ?? "",
            CreatedAt = c.CreatedAt,
            IsPublic = c.IsPublic,
            BookmarkCount = c.BookmarkCategories.Count
        }).ToList();

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
