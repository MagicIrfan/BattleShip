using BattleShip.Models;
namespace BattleShip.Services;

public interface IGameLogicService
{
    Task StartGame();
    Task PlaceBoats();
    Task Attack(Position attackPosition);
    void PlaceBoat(List<Position> positions);
    bool IsBoatAtPosition(Position position);
    Task Rollback();
}


public class GameLogicService : IGameLogicService
{
    private readonly IGameStateService _stateService;
    private readonly IGameApiService _apiService;
    private readonly IGameUIService _uiService;

    public GameLogicService(
        IGameStateService stateService,
        IGameApiService apiService,
        IGameUIService uiService)
    {
        _stateService = stateService;
        _apiService = apiService;
        _uiService = uiService;
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
        await _apiService.PlaceBoatsAsync(_stateService.Boats, _stateService.GameId);
    }

    public async Task Attack(Position attackPosition)
    {
        var attackResponse = await _apiService.AttackAsync(_stateService.GameId, attackPosition);
        _stateService.UpdateGameState(attackResponse);

        await _uiService.HandleEndGameConditions(attackResponse);
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
        var rollbackResponse = await _apiService.RollbackAsync(_stateService.GameId);
        _stateService.RestorePreviousState(rollbackResponse);
    }
}

