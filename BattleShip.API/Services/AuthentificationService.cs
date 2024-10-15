﻿using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BattleShip.API.Services;

public interface IAuthenticationService
{
    Task Login();
    Task Logout();
    Task<IResult> Profile();
}

public class AuthenticationService(IHttpContextAccessor httpContextAccessor) : IAuthenticationService
{
    private HttpContext Context => httpContextAccessor.HttpContext!;
    
    public async Task Login()
    {
        const string returnUrl = "/";
        
        var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(returnUrl)
            .Build();

        await Context.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
    }

    public async Task Logout()
    {
        var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri("/")
            .Build();

        await Context.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties);
        await Context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public Task<IResult> Profile()
    {
        foreach (var claim in Context.User.Claims)
        {
            Console.WriteLine("Claims: " + claim);
        }
    
        var user = Context.User;
    
        var pictureClaim = user.Claims.FirstOrDefault(c => c.Type == "picture");

        return Task.FromResult(Results.Ok(new
        {
            user.Identity?.Name,
            picture = pictureClaim?.Value
        }));
    }
}