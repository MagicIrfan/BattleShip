using BattleShip.Models;

namespace BattleShip.API;

public interface IGameRepository
{
    void AddGame(Guid gameId, GameState gameState);
    GameState? GetGame(Guid gameId);
}

public class GameRepository : IGameRepository
{
    private readonly Dictionary<Guid, GameState> _gameStates = new();

    public void AddGame(Guid gameId, GameState gameState)
    {
        _gameStates[gameId] = gameState;
    }

    public GameState? GetGame(Guid gameId)
    {
        return _gameStates.GetValueOrDefault(gameId);
    }
}