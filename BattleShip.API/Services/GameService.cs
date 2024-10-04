using BattleShip.API.Helpers;
using BattleShip.Models;
using FluentValidation;

namespace BattleShip.API.Services;

public interface IGameService
{
    List<Boat> GenerateRandomBoats();
    Task<(bool isHit, bool isSunk, bool isWinner)> ProcessAttack(AttackRequest attackRequest, IValidator<AttackRequest> validator);
    IResult RollbackTurn(Guid gameId);
    Guid StartGame();
    IResult GetLeaderboard();
    IResult PlaceBoats(List<Boat> playerBoats, Guid gameId);
}

public class GameService(IGameRepository gameRepository, IHttpContextAccessor httpContextAccessor) : IGameService
{
    private const int GridSize = 10;
    private HttpContext Context => httpContextAccessor.HttpContext!;

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
    
    private AttackRequest GenerateIaAttackRequest(GameState gameState)
    {
        var random = new Random();
        var history = gameState.AttackHistory.Where(x => x.PlayerId == "IA").ToList();
    
        var targetPositions = new HashSet<Position>();
    
        if (history.Any(h => h.IsHit))
        {
            var lastHit = history.Last(h => h.IsHit);
            var hitPosition = lastHit.AttackPosition;

            targetPositions.Add(new Position(hitPosition.X - 1, hitPosition.Y)); 
            targetPositions.Add(new Position(hitPosition.X + 1, hitPosition.Y)); 
            targetPositions.Add(new Position(hitPosition.X, hitPosition.Y - 1)); 
            targetPositions.Add(new Position(hitPosition.X, hitPosition.Y + 1)); 
        }

        if (targetPositions.Count == 0)
        {
            while (targetPositions.Count < 5)
            {
                var attackPosition = new Position(random.Next(0, 10), random.Next(0, 10));
                targetPositions.Add(attackPosition);
            }
        }

        var selectedPosition = targetPositions.FirstOrDefault(pos => 
            pos.X is >= 0 and < 10 && pos.Y is >= 0 and < 10 &&
            !history.Any(h => h.AttackPosition.X == pos.X && h.AttackPosition.Y == pos.Y)) ?? new Position(random.Next(0, 10), random.Next(0, 10));

        return new AttackRequest(selectedPosition);
    }


    public async Task<(bool isHit, bool isSunk, bool isWinner)> ProcessAttack(AttackRequest attackRequest, IValidator<AttackRequest> validator)
    {
        var playerId = Context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");

        var validationResult = await validator.ValidateAsync(attackRequest);
        if (!validationResult.IsValid)
            throw new ValidationException("Invalid attack request", validationResult.Errors);

        var gameState = gameRepository.GetGame(attackRequest.GameId);
        if (gameState == null)
            throw new KeyNotFoundException("Game not found");
        
        if (attackRequest.AttackPosition == null) GenerateIaAttackRequest(gameState);

        var boats = playerId == gameState.PlayerOneId 
            ? gameState.PlayerOneBoats 
            : playerId == gameState.PlayerTwoId 
                ? gameState.PlayerTwoBoats 
                : throw new UnauthorizedAccessException("Player not recognized in this game.");

        var (isHit, isSunk, updatedBoats) = AttackHelper.ProcessAttack(boats, attackRequest.AttackPosition);

        var attackRecord = new GameState.AttackRecord(attackRequest.AttackPosition, playerId, isHit, isSunk);
        var isWinner = false;

        if (playerId.Equals(gameState.PlayerOneId))
        {
            gameState.PlayerOneBoats = updatedBoats;
            if (CheckIfAllBoatsSunk(updatedBoats))
            {
                isWinner = true;
                gameState.IsPlayerOneWinner = isWinner;
            }
        }
            
        else if (playerId.Equals(gameState.PlayerTwoId) || gameState.PlayerTwoId!.Equals("IA"))
        {
            gameState.PlayerTwoBoats = updatedBoats;
            if (CheckIfAllBoatsSunk(updatedBoats))
            {
                isWinner = true;
                gameState.IsPlayerTwoWinner = isWinner;
            }
        }

        gameState.AttackHistory.Add(attackRecord);
        gameRepository.UpdateGame(gameState);

        return (isHit, isSunk, isWinner); 
    }

    private bool CheckIfAllBoatsSunk(List<Boat> boats)
    {
        return boats.All(b => b.Positions.All(p => p.IsHit));
    }

    public IResult RollbackTurn(Guid gameId)
    {
        var gameState = gameRepository.GetGame(gameId);
        
        if (gameState == null)
            return Results.NotFound("Game not found");

        if (gameState.AttackHistory.Count == 0)
            return Results.BadRequest("No moves to rollback");
        
        var playerId = Context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");
        
        var lastAttack = gameState.AttackHistory.Last();
        gameState.AttackHistory.RemoveAt(gameState.AttackHistory.Count - 1);

        if (lastAttack.PlayerId.Equals(gameState.PlayerOneId))
            AttackHelper.UndoLastAttack(gameState.PlayerOneBoats, lastAttack);
        else if (lastAttack.PlayerId.Equals(gameState.PlayerTwoId))
            AttackHelper.UndoLastAttack(gameState.PlayerTwoBoats, lastAttack);

        gameRepository.UpdateGame(gameState);
        
        return Results.Ok(new
        {
            gameState.GameId,
            Message = "Last move rolled back successfully."
        });
    }

    public Guid StartGame()
    {
        var gameId = Guid.NewGuid();
        var playerId = Context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");

        var computerBoats = GenerateRandomBoats();

        var gameState = new GameState(
            gameId: gameId,
            playerOneBoats: [],
            playerTwoBoats: computerBoats,
            isPlayerOneWinner: false,
            isPlayerTwoWinner: false,
            playerOneId: playerId,
            playerTwoId: "IA"
        );

        gameRepository.AddGame(gameId, gameState);

        return gameState.GameId;
    }


    public IResult GetLeaderboard()
    {
        var leaderboard = gameRepository.GetLeaderboard();
        return Results.Ok(leaderboard);
    }


    public IResult PlaceBoats(List<Boat> playerBoats, Guid gameId)
    {
        if (playerBoats.Count != 5) 
            return Results.BadRequest("Number of boats should be equal to five.");

        if (playerBoats.Any(boat => !BoatPlacementHelper.PlaceBoat(boat, playerBoats)))
            return Results.BadRequest("Boat placements are impossible.");

        var gameState = gameRepository.GetGame(gameId);
        if (gameState != null) gameState.PlayerOneBoats = playerBoats;
        else return Results.BadRequest("Non-existent game.");
        gameRepository.UpdateGame(gameState);

        return Results.Ok();
    }
}
