using BattleShip.API.Services;
using BattleShip.Models;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;

namespace BattleShip.API;

public class GameHub(IGameService gameService, IValidator<AttackRequest> validator) : Hub
{
    private static readonly Dictionary<Guid, GameState> Games = new();
    
    public async Task JoinGame(Guid gameId)
    {
        var playerId = Context.User?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");
        
        if (!Games.TryGetValue(gameId, out var multiplayerGame))
        {
            var gameState = new GameState(
                gameId: gameId,
                playerOneBoats: [],
                playerTwoBoats: [], 
                isPlayerOneWinner: false,
                isPlayerTwoWinner: false,
                playerOneId: playerId,
                playerTwoId: ""
            )
            {
                IsMultiplayer = true
            };

            Games[gameId] = gameState;
        }
        else
        {
            if (!multiplayerGame.IsFull())
                multiplayerGame.AssignPlayer2(playerId);
            else 
                await Clients.Client(Context.ConnectionId).SendAsync("Game is full");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());

        if (multiplayerGame != null && multiplayerGame.IsFull())
        {
            await Clients.Group(gameId.ToString()).SendAsync("InitializeGame");
        }
    }

    public async Task PlaceBoat(List<Boat> playerBoats, Guid gameId)
    {
        var playerId = Context.User?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");
        
        await gameService.PlaceBoats(playerBoats, gameId);
        await Clients.Group(gameId.ToString()).SendAsync("Boat placed", playerId);
    }

    public async Task SendAttack(Guid gameId, int x, int y)
    {
        var attackerId = Context.User?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

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
    }

    public async Task LeaveGame(Guid gameId, string playerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());
        Games.Remove(gameId);
        await Clients.Group(gameId.ToString()).SendAsync("PlayerLeft", playerId);
    }
}