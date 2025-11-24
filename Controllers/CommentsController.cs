using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;
using markly.Helpers;
using markly.ViewModels;

namespace markly.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CommentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CommentCreateDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest(new { success = false, message = "Comment content is required." });
        }

        if (dto.Content.Length > 2000)
        {
            return BadRequest(new { success = false, message = "Comment is too long (max 2000 characters)." });
        }

        var bookmark = await _context.Bookmarks.FindAsync(dto.BookmarkId);
        if (bookmark == null)
        {
            return NotFound(new { success = false, message = "Bookmark not found." });
        }

        // Allow comments on public bookmarks or bookmarks owned by the user
        if (!bookmark.IsPublic && bookmark.UserId != user.Id)
        {
            return NotFound(new { success = false, message = "Bookmark not found." });
        }

        var comment = new Comment
        {
            Content = dto.Content.Trim(),
            BookmarkId = dto.BookmarkId,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            comment = new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                AuthorName = UserHelper.GetAuthorName(user),
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsOwner = true
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromBody] CommentEditDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest(new { success = false, message = "Comment content is required." });
        }

        if (dto.Content.Length > 2000)
        {
            return BadRequest(new { success = false, message = "Comment is too long (max 2000 characters)." });
        }

        var comment = await _context.Comments.FindAsync(dto.Id);
        if (comment == null)
        {
            return NotFound(new { success = false, message = "Comment not found." });
        }

        if (!IsOwner(comment, user.Id))
        {
            return Forbid();
        }

        comment.Content = dto.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            comment = new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                AuthorName = UserHelper.GetAuthorName(user),
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsOwner = true
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] CommentDeleteDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var comment = await _context.Comments.FindAsync(dto.Id);
        if (comment == null)
        {
            return NotFound(new { success = false, message = "Comment not found." });
        }

        if (!IsOwner(comment, user.Id))
        {
            return Forbid();
        }

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    private static bool IsOwner(Comment comment, string userId)
    {
        return !string.IsNullOrEmpty(userId) && comment.UserId == userId;
    }
}
