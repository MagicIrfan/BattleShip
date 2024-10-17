using System.Security.Claims;
using BattleShip.API.Helpers;
using BattleShip.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.IdentityModel.Tokens;

namespace BattleShip.API.Services;

public interface IGameService
{
    Task<AttackModel.AttackResponse> ProcessAttack(AttackModel.AttackRequest attackRequest,
        IValidator<AttackModel.AttackRequest> validator);

    Task<IResult> RollbackTurn(Guid gameId);
    Task<Guid> StartGame(StartGameRequest request, IValidator<StartGameRequest> validator);
    Task<IResult> GetLeaderboard();
    Task<IResult> PlaceBoats(List<Boat> playerBoats, Guid gameId, IValidator<Boat> validator);
}

public class GameService(IGameRepository gameRepository, IHttpContextAccessor httpContextAccessor) : IGameService
{
    private HttpContext Context => httpContextAccessor.HttpContext!;

    public async Task<AttackModel.AttackResponse> ProcessAttack(AttackModel.AttackRequest attackRequest, IValidator<AttackModel.AttackRequest> validator)
    {
        var playerId = Context.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User not recognized");

        var validationResult = await validator.ValidateAsync(attackRequest);
        if (!validationResult.IsValid)
            throw new ValidationException("Invalid attack request", validationResult.Errors);

        var gameState = gameRepository.GetGame(attackRequest.GameId)
            ?? throw new KeyNotFoundException("Game not found");

        var currentPlayer = gameState.Players.FirstOrDefault(p => p.PlayerId.Equals(playerId));
        if (currentPlayer == null || gameState.Players.Any(p => p.IsPlayerWinner))
            throw new InvalidOperationException("A Player has already won the game.");

        GameHelper.ValidateTurn(gameState, playerId);

        var enemyPlayer = gameState.Players.FirstOrDefault(p => p.PlayerId != playerId);
        if (enemyPlayer == null)
            throw new InvalidOperationException("Enemy player not found.");

        var enemyBoats = enemyPlayer.PlayerBoats;
        var (playerIsHit, playerIsSunk, updatedEnemiesBoats) = AttackHelper.ProcessAttack(enemyBoats, attackRequest.AttackPosition);

        var playerAttackRecord = new GameState.AttackRecord(attackRequest.AttackPosition, playerId, playerIsHit, playerIsSunk);
        var playerIsWinner = GameHelper.UpdateGameState(gameState, playerId, updatedEnemiesBoats, gameRepository);

        gameState.AttackHistory.Add(playerAttackRecord);
        gameRepository.UpdateGame(gameState);

        var response = new AttackModel.AttackResponse
        {
            PlayerIsHit = playerIsHit,
            PlayerIsSunk = playerIsSunk,
            PlayerIsWinner = playerIsWinner,
            PlayerAttackPosition = attackRequest.AttackPosition
        };

        if (!gameState.IsMultiplayer)
        {
            var aiAttackRequest = await IaHelper.GenerateIaAttackRequest(gameState);
            var player = gameState.Players.FirstOrDefault(p => p.PlayerId != "IA");
            var playerBoats = player?.PlayerBoats;

            if (playerBoats == null)
                throw new InvalidOperationException("AI player not found or has no boats.");

            var (aiIsHit, aiIsSunk, updatedPlayerBoats) = AttackHelper.ProcessAttack(playerBoats, aiAttackRequest);
            var aiIsWinner = GameHelper.UpdateGameState(gameState, "IA", updatedPlayerBoats, gameRepository);

            var aiAttackRecord = new GameState.AttackRecord(aiAttackRequest, "IA", aiIsHit, aiIsSunk);
            gameState.AttackHistory.Add(aiAttackRecord);
            gameRepository.UpdateGame(gameState);

            response.AiIsHit = aiIsHit;
            response.AiIsSunk = aiIsSunk;
            response.AiIsWinner = aiIsWinner;
            response.AiAttackPosition = aiAttackRequest;
        }

        return response;
    }

    public Task<IResult> RollbackTurn(Guid gameId)
    {
        var gameState = gameRepository.GetGame(gameId);

        if (gameState == null)
            return Task.FromResult(Results.NotFound("Game not found"));

        if (gameState.IsMultiplayer)
            return Task.FromResult(Results.NoContent());

        if (gameState.Players.Any(player => player.IsPlayerWinner))
            return Task.FromResult(Results.BadRequest("A player has already won the game. Rollback is not allowed."));

        if (gameState.AttackHistory.Count == 0)
            return Task.FromResult(Results.BadRequest("No moves to rollback"));

        var playerId = Context.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");

        var lastAttack = gameState.AttackHistory.Last();
        gameState.AttackHistory.RemoveAt(gameState.AttackHistory.Count - 1);

        var player = gameState.Players.FirstOrDefault(p => p.PlayerId.Equals(lastAttack.PlayerId));
        if (player == null)
            return Task.FromResult(Results.BadRequest("Non-existent player."));

        AttackHelper.UndoLastAttack(player.PlayerBoats, lastAttack);

        gameRepository.UpdateGame(gameState);

        return Task.FromResult(Results.Ok(new
        {
            gameState.GameId,
            Message = "Last move rolled back successfully."
        }));
    }

    public async Task<Guid> StartGame(StartGameRequest request, IValidator<StartGameRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException("Invalid request", validationResult.Errors);
    
        var gameId = Guid.NewGuid();
    
        var playerId = Context.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    
        Console.WriteLine("playerId: " + playerId);

        if (string.IsNullOrEmpty(playerId))
            throw new Exception("User not recognized");

        var computerBoats = GameHelper.GenerateRandomBoats(request.SizeGrid.Value);

        var players = new List<Player>
        {
            new(playerId, [], false), 
            new("IA", computerBoats, false)
        };

        var gameState = new GameState(
            gameId: gameId,
            players: players,                
            difficulty: request.Difficulty
        );

        if (request.SizeGrid.HasValue)
            gameState.GridSize = request.SizeGrid.Value;

        gameRepository.AddGame(gameId, gameState);

        return gameState.GameId;
    }


    public Task<IResult> GetLeaderboard()
    {
        var leaderboard = gameRepository.GetLeaderboard();
        return Task.FromResult(Results.Ok(leaderboard));
    }
    
    public Task<IResult> PlaceBoats(List<Boat> playerBoats, Guid gameId, IValidator<Boat> validator)
    {
        foreach (var validationResult in playerBoats.Select(validator.Validate))
        {
            if (!validationResult.IsValid)
            {
                var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                return Task.FromResult(Results.BadRequest(new { Errors = errorMessages }));
            }
        }

        var playerId = Context.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");

        var gameState = gameRepository.GetGame(gameId);
        if (gameState == null)
            return Task.FromResult(Results.BadRequest("Non-existent game."));

        var player = gameState.Players.FirstOrDefault(p => p.PlayerId.Equals(playerId));
        if (player == null)
            return Task.FromResult(Results.BadRequest("Non-existent player."));

        if (player.PlayerBoats is { Count: > 0 })
            return Task.FromResult(Results.BadRequest("Boats are already placed for the player."));

        if (playerBoats.Count != 5)
            return Task.FromResult(Results.BadRequest("Number of boats should be equal to five."));

        if (!GameHelper.ValidateBoatPositions(playerBoats, gameState.GridSize))
            return Task.FromResult(Results.BadRequest("Boat placements are impossible."));

        player.PlayerBoats = playerBoats;

        gameRepository.UpdateGame(gameState);

        return Task.FromResult(Results.Ok());
    }

}