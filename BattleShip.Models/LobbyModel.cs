namespace BattleShip.Models;

public class LobbyModel(Guid gameId, bool isPrivate)
{
    public Guid GameId { get; set; } = gameId;
    public string? PlayerOneId { get; set; }
    public string? PlayerTwoId { get; set; }
    public bool PlayerOneReady { get; set; }
    public bool PlayerTwoReady { get; set; }
    public bool IsPrivate { get; set; } = isPrivate;
    
    private Dictionary<string, PlayerInfo> PlayerInfo { get; } = new();
    
    public void AssignPlayer(string playerId, string username, string profilePicture)
    {
        var playerInfo = new PlayerInfo()
        {
            Username = username,
            Picture = profilePicture,
            Id = playerId
        };
        
        if (string.IsNullOrEmpty(PlayerOneId))
        {
            PlayerOneId = playerId;
            PlayerInfo[playerId] = playerInfo;
        }
        else if (string.IsNullOrEmpty(PlayerTwoId))
        {
            PlayerTwoId = playerId;
            PlayerInfo[playerId] = playerInfo;
        }
    }

    public bool IsFull() => !string.IsNullOrEmpty(PlayerTwoId) && !string.IsNullOrEmpty(PlayerOneId);
        
    public List<PlayerInfo> GetPlayerList()
    {
        return PlayerInfo.Values.ToList();
    }

    public void RemovePlayer(string playerId)
    {
        if (PlayerOneId == playerId)
        {
            PlayerInfo.Remove(playerId);
            PlayerOneId = null;
        }
        else if (PlayerTwoId == playerId)
        {
            PlayerInfo.Remove(playerId);
            PlayerTwoId = null;
        }
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