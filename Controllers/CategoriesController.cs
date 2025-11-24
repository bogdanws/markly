using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;
using markly.Helpers;
using markly.Models;
using markly.ViewModels;

namespace markly.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<CategoriesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var categories = await _context.Categories
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CategoryFormViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsPublic = c.IsPublic,
                BookmarkCount = c.BookmarkCategories.Count,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return View(categories);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var category = await _context.Categories
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        bool isOwner = currentUser != null && category.UserId == currentUser.Id;

        // Privacy Check
        if (!category.IsPublic && !isOwner)
        {
            return NotFound();
        }

        // Fetch bookmarks
        var bookmarksRaw = await _context.BookmarkCategories
            .Where(bc => bc.CategoryId == id)
            .Where(bc => isOwner || bc.Bookmark.IsPublic)
            .Include(bc => bc.Bookmark)
                .ThenInclude(b => b.User)
            .Include(bc => bc.Bookmark)
                .ThenInclude(b => b.Votes)
            .Select(bc => bc.Bookmark)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
            
        var bookmarks = bookmarksRaw.Select(b => 
        {
            var media = BookmarkMediaContent.FromJson(b.Content);
            return new BookmarkListItemViewModel
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                AuthorName = UserHelper.GetAuthorName(b.User),
                CreatedAt = b.CreatedAt,
                VoteCount = b.Votes.Count,
                MediaImageUrl = media.ImageUrl,
                MediaTextPreview = UserHelper.BuildTextPreview(media.TextContent, 160),
                IsPrivate = !b.IsPublic
            };
        }).ToList();

        var model = new CategoryDetailsViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsPublic = category.IsPublic,
            CreatedAt = category.CreatedAt,
            OwnerId = category.UserId,
            OwnerName = UserHelper.GetAuthorName(category.User),
            IsOwner = isOwner,
            Bookmarks = bookmarks
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Check for duplicate name
        model.Name = model.Name.Trim();
        if (await _context.Categories.AnyAsync(c => c.UserId == user.Id && c.Name.ToLower() == model.Name.ToLower()))
        {
            ModelState.AddModelError("Name", "You already have a category with this name.");
            return View(model);
        }

        var category = new Category
        {
            Name = model.Name,
            Description = model.Description?.Trim(),
            IsPublic = model.IsPublic,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
        if (category == null) return NotFound();

        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsPublic = category.IsPublic
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == model.Id && c.UserId == user.Id);
        if (category == null) return NotFound();

        // Check duplicate name if changed
        model.Name = model.Name.Trim();
        if (category.Name.ToLower() != model.Name.ToLower() && 
            await _context.Categories.AnyAsync(c => c.UserId == user.Id && c.Name.ToLower() == model.Name.ToLower()))
        {
            ModelState.AddModelError("Name", "You already have a category with this name.");
            return View(model);
        }

        category.Name = model.Name;
        category.Description = model.Description?.Trim();
        category.IsPublic = model.IsPublic;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
        if (category == null) return NotFound();

        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            IsPublic = category.IsPublic
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var category = await _context.Categories
            .Include(c => c.BookmarkCategories)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
        if (category == null) return NotFound();

        // Remove associated BookmarkCategories first
        _context.BookmarkCategories.RemoveRange(category.BookmarkCategories);
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuick([FromBody] CategoryFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return BadRequest(new { success = false, message = "Category name is required." });
        }

        model.Name = model.Name.Trim();

        if (model.Name.Length > 50)
        {
            return BadRequest(new { success = false, message = "Category name is too long." });
        }

        // Check for duplicate name
        if (await _context.Categories.AnyAsync(c => c.UserId == user.Id && c.Name.ToLower() == model.Name.ToLower()))
        {
            return BadRequest(new { success = false, message = "Category already exists." });
        }

        var category = new Category
        {
            Name = model.Name,
            IsPublic = model.IsPublic,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, category = new { id = category.Id, name = category.Name } });
    }
}
