namespace markly.ViewModels.Admin;

public class AdminCategoriesViewModel
{
    public List<AdminCategoryItemViewModel> Categories { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public string? SearchQuery { get; set; }
}
