using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;

namespace markly.Controllers;

[Authorize]
public class TagsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TagsController> _logger;

    public TagsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<TagsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Quick create a new global tag via AJAX.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuick([FromBody] CreateTagRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var validationError = ValidateTagName(request.Name);
        if (validationError != null)
        {
            return BadRequest(new { success = false, message = validationError });
        }

        var normalizedName = NormalizeName(request.Name);

        // Check if tag already exists (case-insensitive)
        var existingTag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name.ToLower() == normalizedName.ToLower());

        if (existingTag != null)
        {
            // Return the existing tag instead of creating a duplicate
            return Ok(new { success = true, tag = new { id = existingTag.Id, name = existingTag.Name }, existed = true });
        }

        var tag = new Tag
        {
            Name = normalizedName,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, tag = new { id = tag.Id, name = tag.Name }, existed = false });
    }

    /// <summary>
    /// Bulk create tags from AI suggestions.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBulk([FromBody] CreateBulkTagsRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (request.Names == null || !request.Names.Any())
        {
            return BadRequest(new { success = false, message = "At least one tag name is required." });
        }

        var results = new List<object>();
        var normalizedNames = request.Names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(NormalizeName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Get existing tags
        var existingTags = await _context.Tags
            .Where(t => normalizedNames.Select(n => n.ToLower()).Contains(t.Name.ToLower()))
            .ToListAsync();

        foreach (var name in normalizedNames)
        {
            var validationError = ValidateTagName(name);
            if (validationError != null)
            {
                results.Add(new { name, success = false, message = validationError });
                continue;
            }

            var existingTag = existingTags.FirstOrDefault(t => 
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingTag != null)
            {
                results.Add(new { name, success = true, tag = new { id = existingTag.Id, name = existingTag.Name }, existed = true });
            }
            else
            {
                var newTag = new Tag
                {
                    Name = name,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tags.Add(newTag);
                await _context.SaveChangesAsync();

                results.Add(new { name, success = true, tag = new { id = newTag.Id, name = newTag.Name }, existed = false });
            }
        }

        return Ok(new { success = true, results });
    }

    /// <summary>
    /// Search tags by name prefix for autocomplete.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return Ok(new { success = true, tags = Array.Empty<object>() });
        }

        var tags = await _context.Tags
            .Where(t => t.Name.ToLower().Contains(q.ToLower()))
            .OrderBy(t => t.Name)
            .Take(10)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        return Ok(new { success = true, tags });
    }

    private static string NormalizeName(string name)
    {
        // Convert to lowercase, trim, replace spaces with hyphens
        return name.Trim().ToLower().Replace(" ", "-");
    }

    private static string? ValidateTagName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Tag name is required.";
        }

        var normalizedName = NormalizeName(name);

        if (normalizedName.Length > 50)
        {
            return "Tag name is too long (max 50 characters).";
        }

        if (normalizedName.Length < 2)
        {
            return "Tag name is too short (min 2 characters).";
        }

        // Only allow alphanumeric and hyphens
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedName, @"^[a-z0-9\-]+$"))
        {
            return "Tag name can only contain letters, numbers, and hyphens.";
        }

        return null;
    }

    public class CreateTagRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CreateBulkTagsRequest
    {
        public List<string> Names { get; set; } = new();
    }
}
