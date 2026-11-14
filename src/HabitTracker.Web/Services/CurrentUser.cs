using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using HabitTracker.Domain;

namespace HabitTracker.Web.Services;

/// <summary>Small helper so pages don't repeat claim-reading code.</summary>
public class CurrentUser
{
    private readonly AuthenticationStateProvider _auth;
    public CurrentUser(AuthenticationStateProvider auth) => _auth = auth;

    public async Task<(string Id, string Name, bool IsAdmin)> GetAsync()
    {
        var user = (await _auth.GetAuthenticationStateAsync()).User;
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        return (id, user.Identity?.Name ?? "", user.IsInRole(Roles.Admin));
    }
}
