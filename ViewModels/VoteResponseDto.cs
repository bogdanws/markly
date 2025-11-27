namespace markly.ViewModels;

public class VoteResponseDto
{
    public bool Success { get; set; }
    public bool IsLiked { get; set; }
    public int VoteCount { get; set; }
    public string? Message { get; set; }
}

public class VoteToggleDto
{
    public int BookmarkId { get; set; }
}
