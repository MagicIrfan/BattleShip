using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BattleShip.API.Services;

public interface IAuthenticationService
{
    Task Login();
    Task Logout();
    IResult Profile();
}

public class AuthenticationService(HttpContext context) : IAuthenticationService
{
    public async Task Login()
    {
        const string returnUrl = "/";
        
        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(returnUrl)
            .Build();

        await context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    }

    public async Task Logout()
    {
        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri("/")
            .Build();

        await context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public IResult Profile()
    {
        var user = context.User;
        return Results.Ok(new
        {
            user.Identity?.Name,
            EmailAddress = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
        });
    }
}