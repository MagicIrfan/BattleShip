﻿using BattleShip.API.Services;
using BattleShip.Models;
using Microsoft.AspNetCore.SignalR;

namespace BattleShip.API;

public class GameHub(IGameService gameService) : Hub
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
            );

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

    public async Task SendAttack(Guid gameId, string attackerId, int x, int y)
    {
        if (Games.TryGetValue(gameId, out var multiplayerGame))
        {
            /*var targetBoats = multiplayerGame.Player1Id == attackerId ? multiplayerGame.State.OpponentBoats : multiplayerGame.State.PlayerBoats;
            var isHit = gameService.ProcessAttack(targetBoats, new Models.Position(x, y));

            // Mise à jour de l'état du jeu
            await Clients.Group(gameId.ToString()).SendAsync("AttackResult", attackerId, isHit, x, y);

            // Ajout d'un enregistrement d'attaque à l'historique
            multiplayerGame.State.AttackHistory.Add(new GameState.AttackRecord(new Models.Position(x, y), attackerId == multiplayerGame.Player1Id, isHit));

            // Si tous les bateaux sont coulés
            if (gameService.CheckIfAllBoatsSunk(targetBoats))
            {
                await Clients.Group(gameId.ToString()).SendAsync("GameOver", attackerId);
            }*/
        }
    }

    public async Task LeaveGame(Guid gameId, string playerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());
        Games.Remove(gameId);
        await Clients.Group(gameId.ToString()).SendAsync("PlayerLeft", playerId);
    }
    
    
}