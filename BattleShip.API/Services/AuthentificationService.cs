using System.Net.Http.Headers;
using System.Security.Claims;

namespace BattleShip.API.Services;

public interface IAuthenticationService
{
    Task<IResult> Profile();
}

public class AuthenticationService(IHttpContextAccessor httpContextAccessor) : IAuthenticationService
{
    private HttpContext Context => httpContextAccessor.HttpContext!;
    
    public async Task<IResult> Profile()
    {
        var nameIdentifier = Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(nameIdentifier))
            return Results.Unauthorized();

        var token = ExtractToken(Context);

        if (string.IsNullOrEmpty(token))
            return Results.Unauthorized();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.GetAsync("https://dev-dd243sihmby5ljlg.us.auth0.com/userinfo");

        if (!response.IsSuccessStatusCode)
            return Results.Problem("Failed to retrieve user info.");

        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        if (content == null)
            return Results.Problem("Invalid response format.");

        return Results.Ok(new
        {
            UserName = content["nickname"]?.ToString(),
            Picture = content["picture"]?.ToString()
        });
    }
    
    private string? ExtractToken(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return null;
        }

        var authHeaderValue = authHeader.ToString();
        return authHeaderValue.StartsWith("Bearer ") ? authHeaderValue["Bearer ".Length..].Trim() : null;
    }

}