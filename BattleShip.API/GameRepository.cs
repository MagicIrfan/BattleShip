using BattleShip.Models;
using BattleShip.Models.State;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BattleShip.API;

public interface IGameRepository
{
    void AddGame(Guid gameId, GameState gameState);
    GameState? GetGame(Guid gameId);
    void UpdateGame(GameState gameState);
    ConcurrentDictionary<string, PlayerStats> GetLeaderboard();
    void UpdatePlayerWins(string playerId);
    void UpdatePlayerLosses(string userName);
    void UpdateSunkenBoats(string userName);
}

public class GameRepository : IGameRepository
{
    private readonly Dictionary<Guid, GameState> _gameStates = new();
    private readonly ConcurrentDictionary<string, PlayerStats> _leaderboard = new ConcurrentDictionary<string, PlayerStats>();

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

    public ConcurrentDictionary<string, PlayerStats> GetLeaderboard()
    {
        return _leaderboard;
    }

    public void UpdatePlayerWins(string userName)
    {
        var playerStats = _leaderboard.GetOrAdd(userName, _ => new PlayerStats());
        playerStats.Wins++;
    }

    public void UpdatePlayerLosses(string userName)
    {
        var playerStats = _leaderboard.GetOrAdd(userName, _ => new PlayerStats());
        playerStats.Losses++;
    }

    public void UpdateSunkenBoats(string userName)
    {
        var playerStats = _leaderboard.GetOrAdd(userName, _ => new PlayerStats());
        playerStats.SunkenBoats++;
    }
}