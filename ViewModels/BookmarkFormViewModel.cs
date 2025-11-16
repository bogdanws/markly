using System.ComponentModel.DataAnnotations;

namespace markly.ViewModels;

public class BookmarkFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(150, ErrorMessage = "Title must be 150 characters or less.")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500, ErrorMessage = "Description must be 500 characters or less.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Text Content")]
    [StringLength(2000, ErrorMessage = "Text content must be 2000 characters or less.")]
    public string? TextContent { get; set; }

    [Display(Name = "Image URL")]
    [Url(ErrorMessage = "Please enter a valid image URL.")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Video URL")]
    [Url(ErrorMessage = "Please enter a valid video URL.")]
    public string? VideoUrl { get; set; }

    [Display(Name = "Make bookmark public")]
    public bool IsPublic { get; set; } = true;
}
