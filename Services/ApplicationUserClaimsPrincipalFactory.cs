using System.Security.Claims;
using InkIsle.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace InkIsle.Services;

public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("DisplayName", string.IsNullOrWhiteSpace(user.DisplayName) ? "岛主" : user.DisplayName));
        return identity;
    }
}
