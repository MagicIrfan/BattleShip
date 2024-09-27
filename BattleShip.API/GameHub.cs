using Microsoft.AspNetCore.SignalR;

namespace BattleShip.API;

public class GameHub : Hub
{
    public async Task SendAttack(Guid gameId, string playerId, int x, int y)
    {
        await Clients.Others.SendAsync("ReceiveAttack", gameId, playerId, x, y);
    }

    public async Task UpdateGameState(Guid gameId, object gameState)
    {
        await Clients.All.SendAsync("GameStateUpdated", gameId, gameState);
    }
}