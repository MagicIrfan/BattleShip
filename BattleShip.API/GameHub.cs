using BattleShip.API.Services;
using BattleShip.Models;
using Microsoft.AspNetCore.SignalR;

namespace BattleShip.API;

public class GameHub(IGameService gameService) : Hub
{
    private static readonly Dictionary<Guid, MultiplayerGame> Games = new();

    public async Task JoinGame(Guid gameId, string playerId, string playerName)
    {
        if (!Games.TryGetValue(gameId, out var multiplayerGame))
        {
            // Initialisation de la partie
            var gameState = new GameState(
                gameId: gameId,
                playerBoats: gameService.GenerateRandomBoats(),
                opponentBoats: [], // Initialisation vide pour l'adversaire
                isPlayerWinner: false,
                isOpponentWinner: false
            );

            multiplayerGame = new MultiplayerGame(player1Id: playerId, player1Name: playerName, gameState: gameState);
            Games[gameId] = multiplayerGame;
        }
        else
        {
            // Initialisation pour le joueur 2
            multiplayerGame.AssignPlayer2(playerId, playerName);
            multiplayerGame.State.OpponentBoats = gameService.GenerateRandomBoats(); // Assigner les bateaux de l'adversaire
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());

        if (multiplayerGame.IsFull())
        {
            // Envoyer les bateaux aux deux joueurs
            await Clients.Client(Context.ConnectionId).SendAsync("InitializeGame", multiplayerGame.Player1Id == playerId ? multiplayerGame.State.PlayerBoats : multiplayerGame.State.OpponentBoats);
            await Clients.OthersInGroup(gameId.ToString()).SendAsync("InitializeGame", multiplayerGame.Player1Id == playerId ? multiplayerGame.State.OpponentBoats : multiplayerGame.State.PlayerBoats);

            // Informer que le jeu a commencé
            await Clients.Group(gameId.ToString()).SendAsync("GameStarted", gameId);
        }
    }

    public async Task SendAttack(Guid gameId, string attackerId, int x, int y)
    {
        if (Games.TryGetValue(gameId, out var multiplayerGame))
        {
            var targetBoats = multiplayerGame.Player1Id == attackerId ? multiplayerGame.State.OpponentBoats : multiplayerGame.State.PlayerBoats;
            var isHit = gameService.ProcessAttack(targetBoats, new Models.Position(x, y));

            // Mise à jour de l'état du jeu
            await Clients.Group(gameId.ToString()).SendAsync("AttackResult", attackerId, isHit, x, y);

            // Ajout d'un enregistrement d'attaque à l'historique
            multiplayerGame.State.AttackHistory.Add(new GameState.AttackRecord(new Models.Position(x, y), attackerId == multiplayerGame.Player1Id, isHit));

            // Si tous les bateaux sont coulés
            if (gameService.CheckIfAllBoatsSunk(targetBoats))
            {
                await Clients.Group(gameId.ToString()).SendAsync("GameOver", attackerId);
            }
        }
    }

    public async Task LeaveGame(Guid gameId, string playerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());
        Games.Remove(gameId); // Supprimer la partie
        await Clients.Group(gameId.ToString()).SendAsync("PlayerLeft", playerId);
    }
}