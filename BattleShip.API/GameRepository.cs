using BattleShip.Models.State;

namespace BattleShip.API;

public interface IGameRepository
{
    void AddGame(Guid gameId, GameState gameState);
    GameState? GetGame(Guid gameId);
    void UpdateGame(GameState gameState);
    Dictionary<string, int> GetLeaderboard();
    void UpdatePlayerWins(string playerId);
}

public class GameRepository : IGameRepository
{
    private readonly Dictionary<Guid, GameState> _gameStates = new();
    private readonly Dictionary<string, int> _leaderboard = new();

    public void AddGame(Guid gameId, GameState gameState)
    {
        _gameStates[gameId] = gameState;
    }

    public GameState? GetGame(Guid gameId)
    {
        return _gameStates.GetValueOrDefault(gameId);
    }

    public void UpdateGame(GameState gameState)
    {
        if (_gameStates.ContainsKey(gameState.GameId))
            _gameStates[gameState.GameId] = gameState;
        else
            throw new KeyNotFoundException("Game not found");
    }

    public Dictionary<string, int> GetLeaderboard()
    {
        return _leaderboard;
    }

    public void UpdatePlayerWins(string playerId)
    {
        if (!_leaderboard.TryAdd(playerId, 1))
        {
            _leaderboard[playerId]++;
        }
    }
}