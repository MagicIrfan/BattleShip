using BattleShip.API;
using BattleShip.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var gameStates = new Dictionary<Guid, GameState>();
var gameService = new GameService();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/startGame", () =>
{
    var gameId = Guid.NewGuid();

    var playerBoats = gameService.GenerateRandomBoats();
    var computerBoats = gameService.GenerateRandomBoats();

    var gameState = new GameState(
        gameId: gameId,
        playerBoats: playerBoats,
        computerBoats: computerBoats,
        isPlayerWinner: false,
        isComputerWinner: false
    );
    
    gameStates.Add(gameId, gameState);

    return Results.Ok(new
    {
        gameState.GameId, gameState.PlayerBoats
    });
});

app.MapPost("/attack", (Guid gameId, Position attackPosition, [FromServices] Dictionary<Guid, GameState> gameStates) =>
{
    if (!gameStates.TryGetValue(gameId, out var gameState))
    {
        return Results.NotFound("Game not found");
    }

    var playerAttackResult = gameService.ProcessAttack(gameState.ComputerBoats, attackPosition);

    if (gameState.ComputerBoats.All(b => b.Positions.All(p => p.IsHit)))
    {
        gameState.IsPlayerWinner = true;
        return Results.Ok(new
        {
            GameId = gameState.GameId,
            PlayerAttackResult = "Hit",
            IsPlayerWinner = true,
            IsComputerWinner = false
        });
    }

    var computerAttackPosition = new Position(new Random().Next(0, 10), new Random().Next(0, 10));
    var computerAttackResult = ProcessAttack(gameState.PlayerBoats, computerAttackPosition);

    if (gameState.PlayerBoats.All(b => b.Positions.All(p => p.IsHit)))
    {
        gameState.IsComputerWinner = true;
        return Results.Ok(new
        {
            GameId = gameState.GameId,
            PlayerAttackResult = playerAttackResult ? "Hit" : "Miss",
            ComputerAttackPosition = computerAttackPosition,
            ComputerAttackResult = "Hit",
            IsPlayerWinner = false,
            IsComputerWinner = true
        });
    }

    return Results.Ok(new
    {
        GameId = gameState.GameId,
        PlayerAttackResult = playerAttackResult ? "Hit" : "Miss",
        ComputerAttackPosition = computerAttackPosition,
        ComputerAttackResult = computerAttackResult ? "Hit" : "Miss",
        IsPlayerWinner = false,
        IsComputerWinner = false
    });
});


app.Run();
