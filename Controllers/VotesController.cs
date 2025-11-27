using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;
using markly.ViewModels;

namespace markly.Controllers;

[Authorize]
public class VotesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public VotesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle([FromBody] VoteToggleDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var bookmark = await _context.Bookmarks
            .Include(b => b.Votes)
            .FirstOrDefaultAsync(b => b.Id == dto.BookmarkId);

        if (bookmark == null)
        {
            return NotFound(new VoteResponseDto
            {
                Success = false,
                Message = "Bookmark not found."
            });
        }

        // Allow voting on public bookmarks or bookmarks owned by the user
        if (!bookmark.IsPublic && bookmark.UserId != user.Id)
        {
            return NotFound(new VoteResponseDto
            {
                Success = false,
                Message = "Bookmark not found."
            });
        }

        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(v => v.BookmarkId == dto.BookmarkId && v.UserId == user.Id);

        bool isLiked;

        if (existingVote != null)
        {
            // Unlike: remove the vote
            _context.Votes.Remove(existingVote);
            isLiked = false;
        }
        else
        {
            // Like: add a new vote
            var vote = new Vote
            {
                BookmarkId = dto.BookmarkId,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.Votes.Add(vote);
            isLiked = true;
        }

        await _context.SaveChangesAsync();

        // Get updated vote count
        var voteCount = await _context.Votes.CountAsync(v => v.BookmarkId == dto.BookmarkId);

        return Ok(new VoteResponseDto
        {
            Success = true,
            IsLiked = isLiked,
            VoteCount = voteCount
        });
    }
}
