using System.Security.Claims;
using BattleShip.API.Services;
using BattleShip.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BattleShip.API;

[Authorize]
public class GameHub(IMultiplayerService multiplayerService, IValidator<AttackRequest> validator, IValidator<Boat> boatValidator, IGameRepository gameRepository) : Hub
{
    private static readonly Dictionary<Guid, LobbyModel> Lobbies = new();

    public async Task JoinLobby(Guid gameId)
    {
        await multiplayerService.JoinLobby(gameId, Context);
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
        await multiplayerService.OnDisconnectedAsync(exception, Context);
        await base.OnDisconnectedAsync(exception);
    }

    /*public async Task PlaceBoat(List<Boat> playerBoats, Guid gameId)
    {
        var playerId = Context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        
        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");
        
        await gameService.PlaceBoats(playerBoats, gameId, boatValidator);
        await Clients.Group(gameId.ToString()).SendAsync("Boat placed", playerId);
    }

    public async Task SendAttack(Guid gameId, int x, int y)
    {
        var attackerId = Context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(attackerId))
            throw new UnauthorizedAccessException("User not recognized");

        var attackRequest = new AttackRequest(gameId, new Position(x, y));

        var (isHit, isSunk, isWinner) = await gameService.ProcessAttack(attackRequest, validator);

        await Clients.Group(gameId.ToString()).SendAsync("AttackResult", new 
        {
            AttackerId = attackerId,
            IsHit = isHit,
            IsSunk = isSunk,
            IsWinner = isWinner,
            Position = attackRequest.AttackPosition
        });

        if (isWinner)
        {
            await Clients.Group(gameId.ToString()).SendAsync("GameOver", attackerId);
        }
    }*/
}