using markly.Data.Entities;

namespace markly.ViewModels;

public class UserProfileViewModel
{
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime JoinedDate { get; set; }

    public IReadOnlyList<BookmarkListItemViewModel> Bookmarks { get; set; } = Array.Empty<BookmarkListItemViewModel>();
    public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();
    public bool IsOwner { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
