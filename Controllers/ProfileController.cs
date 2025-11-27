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

        var isOwner = User.Identity?.IsAuthenticated == true &&
                        string.Equals(User.Identity.Name, username, StringComparison.OrdinalIgnoreCase);
        var currentUserId = _userManager.GetUserId(User);

        var rawBookmarks = await _context.Bookmarks
            .Where(b => b.UserId == user.Id && (b.IsPublic || isOwner))
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
                b.Content,
                b.IsPublic,
                IsLikedByCurrentUser = currentUserId != null && b.Votes.Any(v => v.UserId == currentUserId)
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
                MediaTextPreview = Helpers.UserHelper.BuildTextPreview(media.TextContent),
                IsPrivate = !b.IsPublic,
                IsLikedByCurrentUser = b.IsLikedByCurrentUser
            };
        }).ToList();

        var categories = await _context.Categories
            .Where(c => c.UserId == user.Id && (c.IsPublic || isOwner))
            .ToListAsync();

        var viewModel = new UserProfileViewModel
        {
            Username = user.UserName!,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Bio = user.Bio ?? "",
            ProfilePictureUrl = user.ProfilePictureUrl,
            JoinedDate = user.CreatedAt,
            Bookmarks = bookmarks,
            Categories = categories,
            IsOwner = isOwner
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
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
            return View(model);
        }

        var oldProfilePictureUrl = user.ProfilePictureUrl;
        string? newProfilePicturePath = null;

        if (model.ProfilePicture != null)
        {
            if (!IsValidImage(model.ProfilePicture))
            {
                ModelState.AddModelError("ProfilePicture", "Invalid image file. Only JPG, PNG, and GIF are allowed (max 2MB).");
                model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                return View(model);
            }

            try 
            {
                newProfilePicturePath = await _fileStorage.SaveFileAsync(model.ProfilePicture, "images/profiles");
                user.ProfilePictureUrl = "/" + newProfilePicturePath;
            }
            catch (Exception)
            {
                ModelState.AddModelError("ProfilePicture", "File upload failed. Please try again.");
                model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                return View(model);
            }
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Bio = model.Bio;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            // Rollback: Delete the newly uploaded file if DB update fails
            if (!string.IsNullOrEmpty(newProfilePicturePath))
            {
                await _fileStorage.DeleteFileAsync(newProfilePicturePath);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            // Restore original URL for view logic if needed
            model.CurrentProfilePictureUrl = oldProfilePictureUrl;
            return View(model);
        }

        // Success: Delete old profile picture if it exists and we uploaded a new one
        if (!string.IsNullOrEmpty(newProfilePicturePath) && !string.IsNullOrEmpty(oldProfilePictureUrl))
        {
            var oldPath = oldProfilePictureUrl.TrimStart('/');
            await _fileStorage.DeleteFileAsync(oldPath);
        }

        TempData["StatusMessage"] = "Your profile has been updated";
        return RedirectToAction(nameof(Index), new { username = user.UserName });
    }

    private static bool IsValidImage(IFormFile file)
    {
        if (file == null || file.Length == 0 || file.Length > 2 * 1024 * 1024)
            return false;

        Span<byte> header = stackalloc byte[8];
        try
        {
            using var stream = file.OpenReadStream();
            if (stream.Read(header) < 8)
                return false;

            // JPEG: FF D8
            if (header[0] == 0xFF && header[1] == 0xD8) return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return true;

            // GIF: 47 49 46 38
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38) return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}
