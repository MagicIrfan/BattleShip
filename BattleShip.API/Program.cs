using Auth0.AspNetCore.Authentication;
using BattleShip.API;
using BattleShip.API.Services;
using BattleShip.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValidationException = FluentValidation.ValidationException;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGrpc();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();


builder.Services.AddSingleton<IGameRepository, GameRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<AttackRequestValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<AttackRequestValidator>();

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"] ?? string.Empty;
    options.ClientId = builder.Configuration["Auth0:ClientId"] ?? string.Empty;
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        corsPolicyBuilder =>
        {
            corsPolicyBuilder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("grpc-status", "grpc-message");
        });
});

var app = builder.Build();

app.UseCors("AllowAllOrigins");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGrpcWeb();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapGrpcService<GameGrpcService>().EnableGrpcWeb();
app.MapHub<GameHub>("/gameHub");

var gameMethodsGroup = app.MapGroup("/api/game/");

gameMethodsGroup.MapPost("/startGame", [Authorize](IGameService gameService) =>
{
    try
    {
        return Results.Ok(gameService.StartGame());
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

gameMethodsGroup.MapPost("/placeBoats", [Authorize] ([FromBody] List<Boat> playerBoats, [FromQuery] Guid gameId, [FromServices] IGameService gameService) =>
{
    try
    {
        return gameService.PlaceBoats(playerBoats, gameId);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

gameMethodsGroup.MapGet("/leaderboard", [Authorize]([FromServices] IGameService gameService) => gameService.GetLeaderboard());

gameMethodsGroup.MapPost("/rollback", [Authorize] ([FromQuery] Guid gameId,[FromServices] IGameService gameService) =>
{
    try
    {
        return gameService.RollbackTurn(gameId);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

gameMethodsGroup.MapPost("/attack", [Authorize] async (AttackRequest attackRequest, IValidator<AttackRequest> validator,[FromServices] IGameService gameService) =>
{
    try
    {
        var (isHit, isSunk, isWinner) = await gameService.ProcessAttack(attackRequest, validator);
        return Results.Ok(new
        {
            PlayerAttackResult = isHit ? (isSunk ? "Sunk" : "Hit") : "Miss",
            IsPlayerWinner = isWinner
        });
    }
    catch (ValidationException ex)
    {
        return Results.ValidationProblem(ex.Errors.ToDictionary(err => err.PropertyName, err => new[] { err.ErrorMessage }));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.Produces(200)
.Produces(404)
.ProducesValidationProblem();

var authenticationMethodsGroup = app.MapGroup("/api/auth/");

authenticationMethodsGroup.MapGet("/login", async ([FromServices] IAuthenticationService authService) => await authService.Login());
authenticationMethodsGroup.MapPost("/logout", [Authorize] async ([FromServices] IAuthenticationService authService) => await authService.Logout());
authenticationMethodsGroup.MapGet("/profile", [Authorize] ([FromServices] IAuthenticationService authService) => authService.Profile());

app.Run();
