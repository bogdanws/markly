namespace markly.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalBookmarks { get; set; }
    public int TotalComments { get; set; }
    public int TotalCategories { get; set; }
    public int TotalUsers { get; set; }

    public List<AdminBookmarkItemViewModel> RecentBookmarks { get; set; } = new();
    public List<AdminCommentItemViewModel> RecentComments { get; set; } = new();
}

public class AdminBookmarkItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPublic { get; set; }
    public int CommentCount { get; set; }
}

public class AdminCommentItemViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorUserName { get; set; } = string.Empty;
    public int BookmarkId { get; set; }
    public string BookmarkTitle { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminCategoryItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPublic { get; set; }
    public int BookmarkCount { get; set; }
}
