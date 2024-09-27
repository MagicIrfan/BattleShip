using BattleShip.Models;

namespace BattleShip.API;

public interface IGameRepository
{
    Task AddGame(Guid gameId, GameState gameState);
    Task<GameState?>  GetGame(Guid gameId);
    Task UpdateGame(GameState gameState);
}

public class GameRepository : IGameRepository
{
    private readonly Dictionary<Guid, GameState> _gameStates = new();

    public async Task AddGame(Guid gameId, GameState gameState)
    {
        _gameStates[gameId] = gameState;
    }

    public async Task<GameState?> GetGame(Guid gameId)
    {
        return _gameStates.GetValueOrDefault(gameId);
    }

    public async Task UpdateGame(GameState gameState)
    {
        if (_gameStates.ContainsKey(gameState.GameId))
            _gameStates[gameState.GameId] = gameState;
        else
            throw new KeyNotFoundException("Game not found");
    }
}