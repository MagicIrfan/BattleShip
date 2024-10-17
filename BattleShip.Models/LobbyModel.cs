namespace BattleShip.Models;

public class LobbyModel(Guid gameId, string playerOneId)
{
    public Guid GameId { get; set; } = gameId;
    public string? PlayerOneId { get; set; } = playerOneId;
    public string? PlayerTwoId { get; set; }
    public bool PlayerOneReady { get; set; }
    public bool PlayerTwoReady { get; set; }
    
    public void AssignPlayer(string playerId)
    {
        if (string.IsNullOrEmpty(PlayerOneId))
            PlayerOneId = playerId;
        if (string.IsNullOrEmpty(PlayerTwoId))
            PlayerTwoId = playerId;
    }

    public bool IsFull() => !string.IsNullOrEmpty(PlayerTwoId) && !string.IsNullOrEmpty(PlayerOneId);
        
    public List<string> GetPlayerList()
    {
        var players = new List<string>();
        if (!string.IsNullOrEmpty(PlayerOneId))
            players.Add(PlayerOneId);
        if (!string.IsNullOrEmpty(PlayerTwoId))
            players.Add(PlayerTwoId);
        return players;
    }

    public void RemovePlayer(string playerId)
    {
        if (PlayerOneId == playerId)
            PlayerOneId = null;
        else if (PlayerTwoId == playerId)
            PlayerTwoId = null;
    }
    
    public void SetPlayerReady(string playerId)
    {
        if (PlayerOneId == playerId)
            PlayerOneReady = !PlayerOneReady;
        else if (PlayerTwoId == playerId)
            PlayerTwoReady = !PlayerTwoReady;
    }

    
    public List<string?> GetReadyPlayers()
    {
        var readyPlayers = new List<string?>();
        
        if (PlayerOneReady)
            readyPlayers.Add(PlayerOneId);
        if (PlayerTwoReady)
            readyPlayers.Add(PlayerTwoId);

        return readyPlayers.Where(id => !string.IsNullOrEmpty(id)).ToList();
    }
}