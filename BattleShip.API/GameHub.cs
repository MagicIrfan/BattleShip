using BattleShip.API.Services;
using BattleShip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BattleShip.API;

[Authorize]
public class GameHub(IMultiplayerService multiplayerService) : Hub
{
    public async Task JoinLobby(Guid gameId, string username, string picture)
    {
        await multiplayerService.JoinLobby(gameId, username, picture, Context);
    }

    public async Task CreateLobby(Guid gameId, string username, string picture, bool isPrivate)
    {
        await multiplayerService.CreateLobby(gameId, username, picture, isPrivate, Context);
    }

    public async Task SetReady(Guid gameId)
    {
        await multiplayerService.SetReady(gameId, Context);
    }
    
    public async Task LeaveGame(Guid gameId)
    {
        await multiplayerService.LeaveGame(gameId, Context);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await multiplayerService.OnDisconnectedAsync(Context);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task PlaceBoat(List<Boat> playerBoats, Guid gameId)
    {
        await multiplayerService.PlaceBoat(playerBoats, gameId, Context);
    }

    public async Task SendAttack(Guid gameId, Position position)
    {
        await multiplayerService.SendAttack(gameId, position, Context);
    }

    public async Task CheckPlayerTurn(Guid gameId)
    {
        await multiplayerService.CheckPlayerTurn(gameId,Context);
    }

    public async Task SendProfile(Guid gameId, string username, string picture)
    {
        await multiplayerService.SendProfile(gameId, username, picture, Context);
    }
}