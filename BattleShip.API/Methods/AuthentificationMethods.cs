using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BattleShip.API.Methods;

public static class AuthenticationMethods
{
    public static async Task Login(HttpContext context, string returnUrl)
    {
        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(returnUrl)
            .Build();

        await context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    }

    public static async Task Logout(HttpContext context)
    {
        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri("/")
            .Build();

        await context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public static async Task<IResult> Profile(HttpContext context)
    {
        var user = context.User;
        return Results.Ok(new
        {
            user.Identity?.Name,
            EmailAddress = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
        });
    }
}