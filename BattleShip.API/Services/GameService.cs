using BattleShip.API.Helpers;
using BattleShip.Models;
using FluentValidation;
using Microsoft.IdentityModel.Tokens;

namespace BattleShip.API.Services;

public interface IGameService
{
    Task<(bool isHit, bool isSunk, bool isWinner)> ProcessAttack(AttackRequest attackRequest,
        IValidator<AttackRequest> validator);

    IResult RollbackTurn(Guid gameId);
    Guid StartGame();
    IResult GetLeaderboard();
    Task<IResult> PlaceBoats(List<Boat> playerBoats, Guid gameId);
}

public class GameService(IGameRepository gameRepository, IHttpContextAccessor httpContextAccessor) : IGameService
{
    private const int GridSize = 10;
    private HttpContext Context => httpContextAccessor.HttpContext!;

    private Position GenerateIaAttackRequest(GameState gameState)
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
                                   !history.Any(h => h.AttackPosition.X == pos.X && h.AttackPosition.Y == pos.Y)) ??
                               new Position(random.Next(0, 10), random.Next(0, 10));

        return selectedPosition;
    }


    public async Task<(bool isHit, bool isSunk, bool isWinner)> ProcessAttack(AttackRequest attackRequest,
        IValidator<AttackRequest> validator)
    {
        var playerId = Context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                       ?? throw new UnauthorizedAccessException("User not recognized");

        var validationResult = await validator.ValidateAsync(attackRequest);
        if (!validationResult.IsValid)
            throw new ValidationException("Invalid attack request", validationResult.Errors);

        var gameState = gameRepository.GetGame(attackRequest.GameId)
                        ?? throw new KeyNotFoundException("Game not found");

        GameHelper.ValidateTurn(gameState, playerId);

        attackRequest.AttackPosition ??= GenerateIaAttackRequest(gameState);

        var boats = GameHelper.GetPlayerBoats(gameState, playerId);
        var (isHit, isSunk, updatedBoats) = AttackHelper.ProcessAttack(boats, attackRequest.AttackPosition);

        var attackRecord = new GameState.AttackRecord(attackRequest.AttackPosition, playerId, isHit, isSunk);
        var isWinner = GameHelper.UpdateGameState(gameState, playerId, updatedBoats, gameRepository);

        gameState.AttackHistory.Add(attackRecord);
        gameRepository.UpdateGame(gameState);

        return (isHit, isSunk, isWinner);
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

        var computerBoats = GameHelper.GenerateRandomBoats();

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


    public Task<IResult> PlaceBoats(List<Boat> playerBoats, Guid gameId)
    {
        var playerId = Context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");

        var gameState = gameRepository.GetGame(gameId);
        if (gameState == null)
            return Task.FromResult(Results.BadRequest("Non-existent game."));

        if (gameState.PlayerOneId.Equals(playerId) && !gameState.PlayerOneBoats.IsNullOrEmpty() ||
            gameState.PlayerTwoId.Equals(playerId) && !gameState.PlayerTwoBoats.IsNullOrEmpty())
            return Task.FromResult(Results.BadRequest("Boats are already placed for the player."));

        if (playerBoats.Count != 5)
            return Task.FromResult(Results.BadRequest("Number of boats should be equal to five."));

        if (!GameHelper.ValidateBoatPositions(playerBoats))
            return Task.FromResult(Results.BadRequest("Boat placements are impossible."));

        if (gameState.PlayerOneId.Equals(playerId))
            gameState.PlayerOneBoats = playerBoats;
        else if (gameState.PlayerTwoId.Equals(playerId))
            gameState.PlayerTwoBoats = playerBoats;
        else
            return Task.FromResult(Results.BadRequest("Non-existent player."));

        gameRepository.UpdateGame(gameState);

        return Task.FromResult(Results.Ok());
    }
}