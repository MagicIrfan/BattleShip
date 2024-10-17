using System.Security.Claims;
using BattleShip.API.Helpers;
using BattleShip.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.IdentityModel.Tokens;

namespace BattleShip.API.Services;

public interface IGameService
{
    Task<object> ProcessAttack(AttackRequest attackRequest,
        IValidator<AttackRequest> validator);

    Task<IResult> RollbackTurn(Guid gameId);
    Task<Guid> StartGame(StartGameRequest request, IValidator<StartGameRequest> validator);
    Task<IResult> GetLeaderboard();
    Task<IResult> PlaceBoats(List<Boat> playerBoats, Guid gameId, IValidator<Boat> validator);
}

public class GameService(IGameRepository gameRepository, IHttpContextAccessor httpContextAccessor) : IGameService
{
    private HttpContext Context => httpContextAccessor.HttpContext!;

    public async Task<object> ProcessAttack(AttackRequest attackRequest, IValidator<AttackRequest> validator)
{
    var playerId = Context.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                   ?? throw new UnauthorizedAccessException("User not recognized");

    var validationResult = await validator.ValidateAsync(attackRequest);
    if (!validationResult.IsValid)
        throw new ValidationException("Invalid attack request", validationResult.Errors);

    var gameState = gameRepository.GetGame(attackRequest.GameId)
                    ?? throw new KeyNotFoundException("Game not found");

    GameHelper.ValidateTurn(gameState, playerId);

    var playerBoats = GameHelper.GetPlayerBoats(gameState, playerId);
    var (playerIsHit, playerIsSunk, updatedPlayerBoats) = AttackHelper.ProcessAttack(playerBoats, attackRequest.AttackPosition);

    var playerAttackRecord = new GameState.AttackRecord(attackRequest.AttackPosition, playerId, playerIsHit, playerIsSunk);
    var playerIsWinner = GameHelper.UpdateGameState(gameState, playerId, updatedPlayerBoats, gameRepository);

    gameState.AttackHistory.Add(playerAttackRecord);
    gameRepository.UpdateGame(gameState);

    if (!gameState.IsMultiplayer)
    {
        var aiAttackRequest = await IaHelper.GenerateIaAttackRequest(gameState);
        var aiBoats = GameHelper.GetPlayerBoats(gameState, "IA");
        
        var (aiIsHit, aiIsSunk, _) = AttackHelper.ProcessAttack(aiBoats, aiAttackRequest);
        
        var aiAttackRecord = new GameState.AttackRecord(aiAttackRequest, "IA", aiIsHit, aiIsSunk);
        gameState.AttackHistory.Add(aiAttackRecord);
        
        var aiIsWinner = GameHelper.UpdateGameState(gameState, "IA", updatedPlayerBoats, gameRepository);

        gameRepository.UpdateGame(gameState);

        return (playerIsHit, playerIsSunk, playerIsWinner, aiIsHit, aiIsSunk, aiIsWinner, aiAttackRequest);
    }

    return (playerIsHit, playerIsSunk, playerIsWinner, attackRequest.AttackPosition);
}


    public Task<IResult> RollbackTurn(Guid gameId)
    {
        var gameState = gameRepository.GetGame(gameId);

        if (gameState == null)
            return Task.FromResult(Results.NotFound("Game not found"));
        
        if (gameState.IsMultiplayer) 
            return Task.FromResult(Results.NoContent());

        if (gameState.AttackHistory.Count == 0)
            return Task.FromResult(Results.BadRequest("No moves to rollback"));

        var playerId = Context.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(playerId))
            throw new UnauthorizedAccessException("User not recognized");

        var lastAttack = gameState.AttackHistory.Last();
        gameState.AttackHistory.RemoveAt(gameState.AttackHistory.Count - 1);

        if (lastAttack.PlayerId.Equals(gameState.PlayerOneId))
            AttackHelper.UndoLastAttack(gameState.PlayerOneBoats, lastAttack);
        else if (lastAttack.PlayerId.Equals(gameState.PlayerTwoId))
            AttackHelper.UndoLastAttack(gameState.PlayerTwoBoats, lastAttack);

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

        var gameState = new GameState(
            gameId: gameId,
            playerOneBoats: [],
            playerTwoBoats: computerBoats,
            isPlayerOneWinner: false,
            isPlayerTwoWinner: false,
            playerOneId: playerId,
            playerTwoId: "IA",
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
        foreach (var errorMessages in from boat in playerBoats select validator.Validate(boat) into validationResult where !validationResult.IsValid select validationResult.Errors
                     .Select(e => e.ErrorMessage)
                     .ToArray())
        {
            return Task.FromResult(Results.BadRequest(new { Errors = errorMessages }));
        }
        
        var playerId = Context.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        
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

        if (!GameHelper.ValidateBoatPositions(playerBoats, gameState.GridSize))
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