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
}
