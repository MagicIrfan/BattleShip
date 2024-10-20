using BattleShip.Models;
using BattleShip.Services.Game;
namespace BattleShip.Services;

public interface IGameLogicService
{
    Task StartGame();
    Task PlaceBoats();
    Task Attack(Position attackPosition);
    void PlaceBoat(List<Position> positions);
    bool IsBoatAtPosition(Position position);
    Task Rollback();
    Grid GetPlayerGrid();
    Grid GetOpponentGrid();
    bool IsPlacingBoat();
    Guid? GetGameId();
    void TogglePlacingBoat();
    List<string> GetHistorique();
    GameParameter GetGameParameter();
    void SetGameParameter(int size, int difficulty);
    List<Boat> GetBoats();
}


public class GameLogicService : IGameLogicService
{
    private readonly IGameStateService _stateService;
    private readonly IGameApiService _apiService;
    private readonly IGameUIService _uiService;
    private readonly IGameEventService _gameEventService;

    public GameLogicService(
        IGameStateService stateService,
        IGameApiService apiService,
        IGameUIService uiService,
        IGameEventService gameEventService)
    {
        _stateService = stateService;
        _apiService = apiService;
        _uiService = uiService;
        _gameEventService = gameEventService;
    }

    public async Task StartGame()
    {
        int gridSize = _stateService.GameParameter.GridSize;
        int difficulty = _stateService.GameParameter.DifficultyLevel;
        var gameId = await _apiService.StartGameAsync(gridSize, difficulty);
        _stateService.InitializeGame(gameId);
    }

    public async Task PlaceBoats()
    {
        var gameId = GetGameId();
        await _apiService.PlaceBoatsAsync(_stateService.Boats, gameId);
    }

    public async Task Attack(Position attackPosition)
    {
        var gameId = GetGameId();
        var attackResponse = await _apiService.AttackAsync(gameId, attackPosition);
        _stateService.UpdateGameState(attackResponse);

        var result = await _uiService.HandleEndGameConditions(attackResponse);
        if(result != null)
        {
            if (result == "restart")
            {
                _gameEventService.RaiseGameRestarted();
                await StartGame();
            }
            else if (result == "return")
            {
                _uiService.NavigateTo("/");
            }
        }
    }

    public void PlaceBoat(List<Position> positions)
    {
        _stateService.PlaceBoat(positions);
    }

    public bool IsBoatAtPosition(Position position)
    {
        return _stateService.IsBoatAtPosition(position);
    }

    public async Task Rollback()
    {
        var gameId = GetGameId();
        var rollbackResponse = await _apiService.RollbackAsync(gameId);
        _stateService.RestorePreviousState(rollbackResponse);
    }

    public Grid GetPlayerGrid()
    {
        return _stateService.PlayerGrid;
    }

    public Grid GetOpponentGrid()
    {
        return _stateService.OpponentGrid;
    }

    public bool IsPlacingBoat()
    { 
        return _stateService.IsPlacingBoat;
    }

    public Guid? GetGameId()
    {
        return _stateService.GameId;
    }
    public void TogglePlacingBoat()
    {
        _stateService.IsPlacingBoat = !_stateService.IsPlacingBoat;
    }

    public List<string> GetHistorique()
    {
        return _stateService.Historique;
    }

    public GameParameter GetGameParameter()
    {
        return _stateService.GameParameter;
    }

    public void SetGameParameter(int size, int difficulty)
    {
        GameParameter gameParameter = _stateService.GameParameter;
        gameParameter.DifficultyLevel = difficulty;
        gameParameter.GridSize = size;
    }

    public List<Boat> GetBoats()
    {
        return _stateService.Boats;
    }
}

