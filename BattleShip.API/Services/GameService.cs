using BattleShip.API.Helpers;
using BattleShip.Models;
using FluentValidation;

namespace BattleShip.API.Services;

public interface IGameService
{
    List<Boat> GenerateRandomBoats();
    Task<IResult> ProcessAttack(AttackRequest attackRequest, IValidator<AttackRequest> validator);
    bool CheckIfAllBoatsSunk(List<Boat> boats);
    Position GenerateRandomPosition();
    IResult RollbackTurn(Guid gameId);
    IResult StartGame();
    IResult GetLeaderboard();
    IResult PlaceBoats(List<Boat> playerBoats);
}

public class GameService(IGameRepository gameRepository, HttpContext context) : IGameService
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

    public async Task<IResult> ProcessAttack(AttackRequest attackRequest, IValidator<AttackRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(attackRequest);

        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var gameState = gameRepository.GetGame(attackRequest.GameId);
        if (gameState == null)
            return Results.NotFound("Game not found");

        var playerAttackResult = AttackHelper.ProcessAttack(gameState.OpponentBoats, attackRequest.AttackPosition);
        var attackRecord = new GameState.AttackRecord(attackRequest.AttackPosition, isPlayerAttack: true, isHit: playerAttackResult);

        gameState.AttackHistory.Add(attackRecord);
        gameRepository.UpdateGame(gameState);

        return UpdateGameState(playerAttackResult, gameState);
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

    public IResult RollbackTurn(Guid gameId)
    {
        var gameState = gameRepository.GetGame(gameId);
        
        if (gameState == null)
            return Results.NotFound("Game not found");

        if (gameState.AttackHistory.Count == 0)
            return Results.BadRequest("No moves to rollback");
        
        
        var lastAttack = gameState.AttackHistory.Last();
        gameState.AttackHistory.RemoveAt(gameState.AttackHistory.Count - 1);

        AttackHelper.UndoLastAttack(gameState, lastAttack);

        gameRepository.UpdateGame(gameState);
        
        return Results.Ok(new
        {
            gameState.GameId,
            Message = "Last move rolled back successfully."
        });
    }

    private IResult UpdateGameState(bool playerAttackResult, GameState gameState)
    {
        var playerId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
    
        if (playerId == null)
            return Results.Unauthorized();
        
        if (CheckIfAllBoatsSunk(gameState.OpponentBoats))
        {
            gameState.IsPlayerWinner = true;
            gameRepository.UpdatePlayerWins(playerId);
            return Results.Ok(new
            {
                gameState.GameId,
                PlayerAttackResult = "Hit",
                IsPlayerWinner = true,
                IsComputerWinner = false
            });
        }

        var computerAttackPosition = GenerateRandomPosition();
        var computerAttackResult = AttackHelper.ProcessAttack(gameState.PlayerBoats, computerAttackPosition);
        
        var attackRecord = new GameState.AttackRecord(computerAttackPosition, isPlayerAttack: false, isHit: computerAttackResult);
        gameState.AttackHistory.Add(attackRecord);
        gameRepository.UpdateGame(gameState);

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

    public IResult StartGame()
    {
        var gameId = Guid.NewGuid();

        var computerBoats = GenerateRandomBoats();

        var gameState = new GameState(
            gameId: gameId,
            playerBoats: [],
            opponentBoats: computerBoats,
            isPlayerWinner: false,
            isOpponentWinner: false
        );
    
        gameRepository.AddGame(gameId, gameState);

        return Results.Ok(new
        {
            gameState.GameId, gameState.PlayerBoats
        });
    }

    public IResult GetLeaderboard()
    {
        var leaderboard = gameRepository.GetLeaderboard();
        return Results.Ok(leaderboard);
    }


    public IResult PlaceBoats(List<Boat> playerBoats)
    {
        throw new NotImplementedException();
    }
}
