using BattleShip.API;
using BattleShip.API.Services;
using BattleShip.API.Validator;
using BattleShip.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValidationException = FluentValidation.ValidationException;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGrpc();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IMultiplayerService, MultiplayerService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

builder.Services.AddSingleton<IGameRepository, GameRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<AttackRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<StartGameRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<BoatValidator>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://dev-dd243sihmby5ljlg.us.auth0.com/";
    options.Audience = "https://localhost:5134/api";
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

app.UseRouting();

app.UseGrpcWeb();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapGrpcService<GameGrpcService>().EnableGrpcWeb();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<GameHub>("/gameHub");
});

var gameMethodsGroup = app.MapGroup("/api/game/");

gameMethodsGroup.MapPost("/startGame", [Authorize]([FromBody] StartGameRequest request, IValidator<StartGameRequest> validator, [FromServices] IGameService gameService) =>
{
    try
    {
        return Results.Ok(gameService.StartGame(request, validator));
    }
    catch (UnauthorizedAccessException e)
    {
        return Results.Problem(e.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

gameMethodsGroup.MapPost("/placeBoats", [Authorize] async ([FromBody] List<Boat> playerBoats, [FromQuery] Guid gameId, [FromServices] IGameService gameService, [FromServices] BoatValidator boatValidator) =>
{
    try
    {
        return await gameService.PlaceBoats(playerBoats, gameId, boatValidator);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

gameMethodsGroup.MapGet("/leaderboard", [Authorize] async ([FromServices] IGameService gameService) =>
{
    try
    {
        return await gameService.GetLeaderboard();
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message);
    }
});

gameMethodsGroup.MapPost("/rollback", [Authorize] async ([FromQuery] Guid gameId,[FromServices] IGameService gameService) =>
{
    try
    {
        return await gameService.RollbackTurn(gameId);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

gameMethodsGroup.MapPost("/attack", [Authorize] async ([FromBody] AttackModel.AttackRequest attackRequest, IValidator<AttackModel.AttackRequest> validator,[FromServices] IGameService gameService) =>
{
    try
    {
        var attackResponse = await gameService.ProcessAttack(attackRequest, validator);
        return Results.Ok(attackResponse);
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
authenticationMethodsGroup.MapGet("/profile", [Authorize] async ([FromServices] IAuthenticationService authService) => await authService.Profile());

var multiplayerMethodsGroup = app.MapGroup("/api/multiplayer/");
multiplayerMethodsGroup.MapGet("/lobbies", [Authorize] async ([FromServices] IMultiplayerService multiplayerService) => await multiplayerService.GetAvailableLobbies());

app.Run();
