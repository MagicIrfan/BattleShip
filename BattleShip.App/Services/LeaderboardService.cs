using BattleShip.Models;
using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BattleShip.Services;

public interface ILeaderboardService
{
    public Task<ConcurrentDictionary<string, PlayerStats>> GetLeaderboard();
}

public class LeaderboardService : ILeaderboardService
{
    public ConcurrentDictionary<string, PlayerStats> Leaderboard { get; set; }

    private readonly IHttpService _httpService;
    public LeaderboardService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<ConcurrentDictionary<string, PlayerStats>> GetLeaderboard()
    {
        var response = await _httpService.SendHttpRequestAsync(HttpMethod.Get, "/game/leaderboard");
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error calling leaderboard: {response.StatusCode}");
        }
        var json = await response.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<ConcurrentDictionary<string, PlayerStats>>(json);

        return leaderboard ?? new ConcurrentDictionary<string, PlayerStats>();
    }
}
