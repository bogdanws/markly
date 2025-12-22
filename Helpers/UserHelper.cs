using markly.Data.Entities;

namespace markly.Helpers;

public static class UserHelper
{
    public static string GetAuthorName(ApplicationUser? user)
    {
        if (user == null)
        {
            return "Unknown";
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return user.UserName ?? "Unknown";
    }

    public static string GetAuthorFirstName(ApplicationUser? user)
    {
        if (user == null)
        {
            return "Unknown";
        }

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            return user.FirstName;
        }

        return user.UserName ?? "Unknown";
    }

    public static string? BuildTextPreview(string? textContent, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(textContent))
        {
            return null;
        }

        var trimmed = textContent.Trim();
        return trimmed.Length <= maxLength ? trimmed : $"{trimmed.Substring(0, maxLength)}...";
    }
}
