namespace markly.ViewModels.Admin;

public class AdminCommentsViewModel
{
    public List<AdminCommentItemViewModel> Comments { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public string? SearchQuery { get; set; }
}
