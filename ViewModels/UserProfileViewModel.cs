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
    
    public IReadOnlyList<BookmarkListItemViewModel> PublicBookmarks { get; set; } = Array.Empty<BookmarkListItemViewModel>();
    public IReadOnlyList<Category> PublicCategories { get; set; } = Array.Empty<Category>();
    
    public string FullName => $"{FirstName} {LastName}".Trim();
}
