using System.Security.Claims;

namespace JanaApi.Utilities;

public static class JwtClaimsHelper
{
    public static int GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("user_id");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }

        return -1; // Return -1 to indicate invalid/missing user ID
    }

    public static string GetUsername(ClaimsPrincipal user)
    {
        var usernameClaim = user.FindFirst("username");
        return usernameClaim?.Value ?? string.Empty;
    }

    public static bool IsAdmin(ClaimsPrincipal user)
    {
        var adminClaim = user.FindFirst("admin");
        if (adminClaim != null && bool.TryParse(adminClaim.Value, out bool isAdmin))
        {
            return isAdmin;
        }

        return false;
    }
}