using markly.Data;
using markly.Data.Entities;
using markly.Services.Interfaces;
using markly.ViewModels;
using markly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace markly.Controllers;

public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IFileStorageService fileStorage)
    {
        _userManager = userManager;
        _context = context;
        _fileStorage = fileStorage;
    }

    [HttpGet("u/{username}")]
    public async Task<IActionResult> Index(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            return NotFound();
        }

        var rawBookmarks = await _context.Bookmarks
            .Where(b => b.UserId == user.Id && b.IsPublic)
            .Include(b => b.User)
            .Include(b => b.Votes)
            .OrderByDescending(b => b.CreatedAt)
            .Take(20)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.Description,
                UserName = b.User.UserName,
                b.CreatedAt,
                VoteCount = b.Votes.Count,
                b.Content
            })
            .ToListAsync();

        var bookmarks = rawBookmarks.Select(b =>
        {
            var media = BookmarkMediaContent.FromJson(b.Content);
            return new BookmarkListItemViewModel
            {
                Id = b.Id,
                Title = b.Title,
                Description = b.Description,
                AuthorName = b.UserName ?? "Unknown",
                CreatedAt = b.CreatedAt,
                VoteCount = b.VoteCount,
                MediaImageUrl = media.ImageUrl,
                MediaTextPreview = media.TextContent != null && media.TextContent.Length > 100 
                    ? media.TextContent.Substring(0, 100) + "..." 
                    : media.TextContent
            };
        }).ToList();

        var categories = await _context.Categories
            .Where(c => c.UserId == user.Id && c.IsPublic)
            .ToListAsync();

        var viewModel = new UserProfileViewModel
        {
            Username = user.UserName!,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Bio = user.Bio ?? "",
            ProfilePictureUrl = user.ProfilePictureUrl,
            JoinedDate = user.CreatedAt,
            PublicBookmarks = bookmarks,
            PublicCategories = categories
        };

        return View(viewModel);
    }

    [Authorize]
    [HttpGet("profile/edit")]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var model = new EditProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Bio = user.Bio,
            CurrentProfilePictureUrl = user.ProfilePictureUrl
        };

        return View(model);
    }

    [Authorize]
    [HttpPost("profile/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Bio = model.Bio;

        if (model.ProfilePicture != null)
        {
            // Validation for image
            if (model.ProfilePicture.Length > 2 * 1024 * 1024) // 2MB
            {
                ModelState.AddModelError("ProfilePicture", "File size must not exceed 2MB.");
                return View(model);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("ProfilePicture", "Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed.");
                return View(model);
            }

            // Store old profile picture URL to delete after successful upload
            var oldProfilePictureUrl = user.ProfilePictureUrl;

            // Save new profile picture
            var path = await _fileStorage.SaveFileAsync(model.ProfilePicture, "images/profiles");
            user.ProfilePictureUrl = "/" + path;

            // Delete old profile picture if it exists
            if (!string.IsNullOrEmpty(oldProfilePictureUrl))
            {
                // Remove leading slash for file storage service
                var oldPath = oldProfilePictureUrl.TrimStart('/');
                await _fileStorage.DeleteFileAsync(oldPath);
            }
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        TempData["StatusMessage"] = "Your profile has been updated";
        return RedirectToAction(nameof(Index), new { username = user.UserName });
    }
}
