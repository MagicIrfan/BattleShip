using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BattleShip.Services;

public interface ITokenService
{
    Task<string> GetAccessTokenAsync();
}

public class TokenService : ITokenService
{
    private readonly ISessionStorageService _sessionStorage;
    private readonly IAccessTokenProvider _tokenProvider;

    public TokenService(ISessionStorageService sessionStorage, IAccessTokenProvider tokenProvider)
    {
        _sessionStorage = sessionStorage;
        _tokenProvider = tokenProvider;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var tokenResult = await _tokenProvider.RequestAccessToken();
        tokenResult.TryGetToken(out var token);
        return token!.Value;
    }
}
