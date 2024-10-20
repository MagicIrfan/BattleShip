using Microsoft.AspNetCore.SignalR.Client;

namespace BattleShip.Services.Multiplayer;

public class SignalRService
{
    private HubConnection _hubConnection;
    private readonly ITokenService _tokenService;

    public SignalRService(ITokenService tokenService)
    {
        _tokenService = tokenService;
        CreateHubConnection();
    }

    private async void CreateHubConnection()
    {
        var token = await _tokenService.GetAccessTokenAsync();
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:5134/gameHub", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .Build();

        await _hubConnection.StartAsync();
    }

    public HubConnection GetConnection() => _hubConnection;
}