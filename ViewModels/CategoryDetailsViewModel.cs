using markly.Data.Entities;

namespace markly.ViewModels;

public class CategoryDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }

    public IReadOnlyList<BookmarkListItemViewModel> Bookmarks { get; set; } = Array.Empty<BookmarkListItemViewModel>();
}
