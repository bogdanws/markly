using System.ComponentModel.DataAnnotations;

namespace markly.ViewModels;

public class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(50, ErrorMessage = "Name must be 50 characters or less.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Description must be 200 characters or less.")]
    public string? Description { get; set; }

    [Display(Name = "Public")]
    public bool IsPublic { get; set; } = false;

    public int BookmarkCount { get; set; }

    // Preview image URLs from bookmarks in this category (up to 4 images for the preview mosaic)
    public List<string> PreviewImages { get; set; } = new();

    public DateTime? CreatedAt { get; set; }
}
