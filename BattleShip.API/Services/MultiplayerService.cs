using System.Security.Claims;
using BattleShip.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BattleShip.API.Services;

public interface IMultiplayerService
{
    Task JoinLobby(Guid gameId, HubCallerContext context);
    Task SetReady(Guid gameId, HubCallerContext context);
    Task LeaveGame(Guid gameId, HubCallerContext context);
    Task OnDisconnectedAsync(Exception? exception, HubCallerContext context);
}

public class MultiplayerService(IHubContext<GameHub> gameHub, IGameRepository gameRepository, IAuthenticationService authenticationService) : IMultiplayerService
{
    private static readonly Dictionary<Guid, LobbyModel> Lobbies = new();
    
    public async Task JoinLobby(Guid gameId, HubCallerContext context)
    {
        var playerId = context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");

        var profileResult = await authenticationService.Profile();

        if (profileResult is Ok<object> okResult)
        {
            var playerInfo = okResult.Value as Dictionary<string, object>;
            var username = playerInfo?["UserName"].ToString();
            var picture = playerInfo?["Picture"].ToString();
        }
        
        LobbyModel lobby;

        if (Lobbies.TryGetValue(gameId, out var value))
        {
            lobby = value;
            if (lobby.PlayerOneId == playerId || lobby.PlayerTwoId == playerId)
            {
                await gameHub.Clients.Client(context.ConnectionId).SendAsync("AlreadyJoin");
                return;
            }
            
            if (!lobby.IsFull())
            {
                lobby.AssignPlayer(playerId);
                Lobbies[gameId] = lobby;
            }
            else
            {
                await gameHub.Clients.Client(context.ConnectionId).SendAsync("GameIsFull");
                return;
            }
        }
        else
        {
            lobby = new LobbyModel(
                gameId: gameId,
                playerOneId: playerId
            );

            Lobbies[gameId] = lobby;
        }

        await gameHub.Groups.AddToGroupAsync(context.ConnectionId, gameId.ToString());
        var currentPlayers = lobby.GetPlayerList();
        await gameHub.Clients.Group(gameId.ToString()).SendAsync("UpdatePlayerList", currentPlayers);
    }
    
    public async Task SetReady(Guid gameId, HubCallerContext context)
    {
        var playerId = context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");
    
        if (Lobbies.TryGetValue(gameId, out var lobby))
        {
            lobby.SetPlayerReady(playerId);
            var readyPlayers = lobby.GetReadyPlayers(); 
            await gameHub.Clients.Group(gameId.ToString()).SendAsync("UpdateReadyStatus", readyPlayers);

            if (readyPlayers.Count == 2)
            {
                Lobbies.Remove(gameId);
                await StartGame(lobby, gameId);
            }
        }
    }
    
    private async Task StartGame(LobbyModel lobby, Guid gameId)
    {
        var gameState = new GameState(
            gameId: gameId,
            playerOneBoats: [],
            playerTwoBoats: [],
            isPlayerOneWinner: false,
            isPlayerTwoWinner: false,
            playerOneId: lobby.PlayerOneId!,
            playerTwoId: lobby.PlayerTwoId!,
            difficulty: 0
        )
        {
            IsMultiplayer = true
        };

        gameRepository.AddGame(gameId, gameState);
        await gameHub.Clients.Group(gameId.ToString()).SendAsync("InitializeGame");
    }
    
    public async Task LeaveGame(Guid gameId, HubCallerContext context)
    {
        var playerId = context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");

        if (Lobbies.TryGetValue(gameId, out var lobby))
        {
            await gameHub.Groups.RemoveFromGroupAsync(context.ConnectionId, gameId.ToString());

            lobby.RemovePlayer(playerId);

            var remainingPlayers = lobby.GetPlayerList();
            await gameHub.Clients.Group(gameId.ToString()).SendAsync("UpdatePlayerList", remainingPlayers);

            if (remainingPlayers.Count == 0)
            {
                Lobbies.Remove(gameId);
            }
        } 
        
        if (gameRepository.GetGame(gameId) != null)
        {
            var gameState = gameRepository.GetGame(gameId);
            await gameHub.Groups.RemoveFromGroupAsync(context.ConnectionId, gameId.ToString());

            if (gameState!.PlayerOneId == playerId)
                gameState.IsPlayerTwoWinner = true;
            else if (gameState.PlayerTwoId == playerId)
                gameState.IsPlayerOneWinner = true;
            
            gameRepository.UpdateGame(gameState);
        } 
    }
    
    public async Task OnDisconnectedAsync(Exception? exception, HubCallerContext context)
    {
        var playerId = context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(playerId))
            return;

        foreach (var gameId in Lobbies.Keys.ToList())
        {
            if (Lobbies.TryGetValue(gameId, out var lobby))
            {
                if (lobby.GetPlayerList().Contains(playerId))
                {
                    await gameHub.Groups.RemoveFromGroupAsync(context.ConnectionId, gameId.ToString());

                    lobby.RemovePlayer(playerId);

                    var remainingPlayers = lobby.GetPlayerList();
                    await gameHub.Clients.Group(gameId.ToString()).SendAsync("UpdatePlayerList", remainingPlayers);

                    if (remainingPlayers.Count == 0)
                    {
                        Lobbies.Remove(gameId);
                    }
                    break;
                }
            }
        }
    }
}