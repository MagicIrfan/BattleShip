using BattleShip.API.Helpers;
using BattleShip.Models;

namespace BattleShip.API.Services;

public interface IGameService
{
    List<Boat> GenerateRandomBoats();
    bool ProcessAttack(List<Boat> boats, Position attackPosition);
    bool CheckIfAllBoatsSunk(List<Boat> boats);
    Position GenerateRandomPosition();
    Task RollbackTurnAsync(GameState gameState, Guid gameId);
    Task<IResult> UpdateGameStateAsync(bool playerAttackResult, GameState gameState);
    Task<IResult> StartGame();
}

public class GameService(IGameRepository gameRepository) : IGameService
{
    private const int GridSize = 10;

    public List<Boat> GenerateRandomBoats()
    {
        var random = new Random();
        var boats = new List<Boat>();

        while (boats.Count < 2) 
        {
            var startPosition = new Position(random.Next(0, GridSize), random.Next(0, GridSize));
            var isVertical = random.Next(0, 2) == 0; 
            var size = boats.Count == 0 ? 3 : 4;

            var boat = new Boat(boats.Count == 0 ? "Destroyer" : "Cruiser", startPosition, isVertical, size);

            if (BoatPlacementHelper.PlaceBoat(boat, boats)) 
                boats.Add(boat);
        }

        return boats;
    }

    public bool ProcessAttack(List<Boat> boats, Position attackPosition)
    {
        return AttackHelper.ProcessAttack(boats, attackPosition);
    }

    public bool CheckIfAllBoatsSunk(List<Boat> boats)
    {
        return boats.All(b => b.Positions.All(p => p.IsHit));
    }

    public Position GenerateRandomPosition()
    {
        var random = new Random();
        return new Position(random.Next(0, GridSize), random.Next(0, GridSize));
    }

    public async Task RollbackTurnAsync(GameState gameState, Guid gameId)
    {
        var lastAttack = gameState.AttackHistory.Last();
        gameState.AttackHistory.RemoveAt(gameState.AttackHistory.Count - 1);

        AttackHelper.UndoLastAttack(gameState, lastAttack);

        await gameRepository.UpdateGame(gameState);
    }

    public async Task<IResult> UpdateGameStateAsync(bool playerAttackResult, GameState gameState)
    {
        if (CheckIfAllBoatsSunk(gameState.OpponentBoats))
        {
            gameState.IsPlayerWinner = true;
            return Results.Ok(new
            {
                gameState.GameId,
                PlayerAttackResult = "Hit",
                IsPlayerWinner = true,
                IsComputerWinner = false
            });
        }

        var computerAttackPosition = GenerateRandomPosition();
        var computerAttackResult = ProcessAttack(gameState.PlayerBoats, computerAttackPosition);
        
        var attackRecord = new GameState.AttackRecord(computerAttackPosition, isPlayerAttack: false, isHit: computerAttackResult);
        gameState.AttackHistory.Add(attackRecord);
        await gameRepository.UpdateGame(gameState);

        if (CheckIfAllBoatsSunk(gameState.PlayerBoats))
        {
            gameState.IsOpponentWinner = true;
            return Results.Ok(new
            {
                gameState.GameId,
                PlayerAttackResult = playerAttackResult ? "Hit" : "Miss",
                ComputerAttackPosition = computerAttackPosition,
                ComputerAttackResult = "Hit",
                IsPlayerWinner = false,
                IsComputerWinner = true
            });
        }

        return Results.Ok(new
        {
            gameState.GameId,
            PlayerAttackResult = playerAttackResult ? "Hit" : "Miss",
            ComputerAttackPosition = computerAttackPosition,
            ComputerAttackResult = computerAttackResult ? "Hit" : "Miss",
            IsPlayerWinner = false,
            IsComputerWinner = false,
            gameState.AttackHistory
        });
    }

    public async Task<IResult> StartGame()
    {
        var gameId = Guid.NewGuid();

        var playerBoats = GenerateRandomBoats();
        var computerBoats = GenerateRandomBoats();

        var gameState = new GameState(
            gameId: gameId,
            playerBoats: playerBoats,
            opponentBoats: computerBoats,
            isPlayerWinner: false,
            isOpponentWinner: false
        );
    
        await gameRepository.AddGame(gameId, gameState);

        return Results.Ok(new
        {
            gameState.GameId, gameState.PlayerBoats
        });
    }
}
