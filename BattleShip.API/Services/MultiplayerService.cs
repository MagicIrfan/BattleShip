using System.Security.Claims;
using BattleShip.Models;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;

namespace BattleShip.API.Services;

public interface IMultiplayerService
{
    Task JoinLobby(Guid gameId, string username, string picture, HubCallerContext context);
    Task CreateLobby(Guid gameId, string username, string picture, bool isPrivate, HubCallerContext context);
    Task SetReady(Guid gameId, HubCallerContext context);
    Task LeaveGame(Guid gameId, HubCallerContext context);
    Task OnDisconnectedAsync(HubCallerContext context);
    Task<List<LobbyModel>> GetAvailableLobbies();
    Task PlaceBoat(List<Boat> playerBoats, Guid gameId, HubCallerContext context);
    Task SendAttack(Guid gameId, int x, int y, HubCallerContext context);
}

public class MultiplayerService(IHubContext<GameHub> gameHub, IGameRepository gameRepository, IGameService gameService, IValidator<Boat> boatValidator, IValidator<AttackModel.AttackRequest> attackValidator) : IMultiplayerService
{
    private static readonly Dictionary<Guid, LobbyModel> Lobbies = new();
    
    public async Task SendAttack(Guid gameId, int x, int y, HubCallerContext context)
    {
        var attackerId = context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(attackerId))
            throw new UnauthorizedAccessException("User not recognized");

        var attackRequest = new AttackModel.AttackRequest(gameId, new Position(x, y));

        var attackResponse = await gameService.ProcessAttack(attackRequest, attackValidator);

        await gameHub.Clients.Group(gameId.ToString()).SendAsync("AttackResult", new 
        {
            attackResponse
        });
    }
    
    public async Task PlaceBoat(List<Boat> playerBoats, Guid gameId, HubCallerContext context)
    {
        var playerId = context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");
        
        await gameService.PlaceBoats(playerBoats, gameId, boatValidator);
        await gameHub.Clients.Group(gameId.ToString()).SendAsync("BoatPlaced", playerId);
        
        if (gameRepository.GetGame(gameId)!.Players.All(p => p.PlayerBoats.Count == 5))
        {
            await gameHub.Clients.Group(gameId.ToString()).SendAsync("BothPlayersReady");
        }
    }
    
    public async Task JoinLobby(Guid gameId, string username, string picture, HubCallerContext context)
    {
        var playerId = context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");

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
                lobby.AssignPlayer(playerId, username, picture);
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
            return;
        }

        await gameHub.Groups.AddToGroupAsync(context.ConnectionId, gameId.ToString());
        var currentPlayers = lobby.GetPlayerList();
        await gameHub.Clients.Group(gameId.ToString()).SendAsync("UpdatePlayerList", currentPlayers);
    }

    public async Task CreateLobby(Guid gameId, string username, string picture, bool isPrivate, HubCallerContext context)
    {
        var playerId = context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");
        
        var lobby = new LobbyModel(
            gameId: gameId,
            isPrivate: isPrivate
        );
        
        lobby.AssignPlayer(playerId, username, picture);

        Lobbies[gameId] = lobby;

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
        var players = new List<Player>
        {
            new(lobby.PlayerOneId!, [], false),
            new(lobby.PlayerTwoId!, [], false)
        };

        var gameState = new GameState(
            gameId: gameId,
            players: players,
            difficulty: 0
        )
        {
            IsMultiplayer = true
        };

        gameRepository.AddGame(gameId, gameState);
        await gameHub.Clients.Group(gameId.ToString()).SendAsync("InitializeGame");
    }
    
    public async Task<List<LobbyModel>> GetAvailableLobbies()
    {
        return await Task.FromResult(Lobbies.Values.Where(lobby => !lobby.IsFull() && !lobby.IsPrivate).ToList());
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
    
        var gameState = gameRepository.GetGame(gameId);
        if (gameState != null)
        {
            await gameHub.Groups.RemoveFromGroupAsync(context.ConnectionId, gameId.ToString());

            var player = gameState.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                player.IsPlayerWinner = true; 
                gameRepository.UpdateGame(gameState);
            }
        } 
    }

    
    public async Task OnDisconnectedAsync(HubCallerContext context)
    {
        var playerId = context.User?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(playerId))
            return;

        foreach (var gameId in Lobbies.Keys.ToList())
        {
            if (Lobbies.TryGetValue(gameId, out var lobby))
            {
                if (lobby.GetPlayerList().Any(p => p.Id == playerId))
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