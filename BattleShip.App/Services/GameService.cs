﻿using BattleShip.Grpc;
using BattleShip.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BattleShip.Services;

public interface IGameService
{

    Guid? gameId { get; set; }
    List<Boat>? boats { get; set; }
    Grid playerGrid { get; set; }
    Grid oponnentGrid { get; set; }
    Task StartGame();
    Task Attack(Position position);
    Task HandleGameResult(AttackResponse responseData);
    void InitializeBoatPositions();
    bool IsBoatAtPosition(Position position);
    Boat? GetBoatAtPosition(Position position);
    string GetBoatSymbol(Position position);
    string GetCellColor(Position position);
    bool IsFinished(AttackResponse responseData);
}

public class GameService(IGameModalService modalService, NavigationManager navManager, ITokenService tokenService, HttpClient httpClient) : IGameService
{
    public Guid? gameId { get; set; }
    public List<Boat>? boats { get; set; }
    public required Grid playerGrid { get; set; }
    public required Grid oponnentGrid { get; set; }
    private Dictionary<Position, Boat> boatPositions = new Dictionary<Position, Boat>();

    private readonly IGameModalService _modalService = modalService;
    private readonly NavigationManager _navManager = navManager;
    private readonly ITokenService _tokenService = tokenService;
    private readonly HttpClient _httpClient = httpClient;

    public async Task Attack(Position position)
    {
        var attackRequest = new AttackRequest
        {
            GameId = gameId?.ToString(),
            AttackPosition = position
        };

        /*var attackResponse = await _gameClient.AttackAsync(attackRequest);

        if (attackResponse != null)
        {
            if (attackResponse.PlayerAttackResult == "Hit")
            {
                position.IsHit = true;
                oponnentGrid.Positions[position.X][position.Y] = new PositionWrapper(position);
            }
            if (attackResponse.ComputerAttackResult == "Hit")
            {
                Position playerBoatPosition = attackResponse.ComputerAttackPosition;
                playerBoatPosition.IsHit = true;
                playerGrid.Positions[playerBoatPosition.X][playerBoatPosition.Y] = new PositionWrapper(playerBoatPosition);
            }
            await HandleGameResult(attackResponse);
        }*/
    }

    public Boat? GetBoatAtPosition(Position position)
    {
        boatPositions.TryGetValue(position, out var boat);
        return boat;
    }

    public string GetBoatSymbol(Position position)
    {
        Boat? boat = GetBoatAtPosition(position);
        return boat != null ? boat.Name[0].ToString() : "";
    }

    public string GetCellColor(Position position)
    {
        return position != null && position.IsHit ? "red" : "white";
    }

    public async Task HandleGameResult(AttackResponse responseData)
    {
        if (IsFinished(responseData))
        {
            string title = "";
            string message = "";
            if (responseData.IsPlayerWinner)
            {
                title = "Victoire !";
                message = "Vous avez gagné !";
            }
            if (responseData.IsComputerWinner)
            {
                title = "Défaite";
                message = "Vous avez perdu";
            }

            var modalResult = await _modalService.ShowModal(title, message);

            if (modalResult == "restart")
            {
                //await StartGame(token);
            }
            else if (modalResult == "return")
            {
                _navManager.NavigateTo("/");
            }
        }
    }

    public void InitializeBoatPositions()
    {
        boatPositions.Clear();
        foreach (var boat in boats)
        {
            foreach (var position in boat.Positions)
            {
                boatPositions[position] = boat;
            }
        }
    }

    public bool IsBoatAtPosition(Position position)
    {
        return boatPositions.ContainsKey(position);
    }

    public bool IsFinished(AttackResponse responseData)
    {
        return responseData.IsPlayerWinner || responseData.IsComputerWinner;
    }

    /*public async Task StartGame()
    {
        var token = await _tokenService.GetAccessTokenAsync();
        var startGameRequest = new StartGameRequest();
        var headers = new Metadata();
        headers.Add("Authorization", $"Bearer {token}");
        var startGameResponse = await _gameClient.StartGameAsync(startGameRequest,headers);

        playerGrid = new Grid(10, 10);
        oponnentGrid = new Grid(10, 10);

        if (startGameResponse != null)
        {
            gameId = new Guid(startGameResponse.GameId);
            boats = startGameResponse.PlayerBoats.ToList();
        }
        InitializeBoatPositions();
    }*/
    public async Task StartGame()
    {
        var token = await _tokenService.GetAccessTokenAsync();
        Console.WriteLine(token);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5134/api/game/startGame");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var responseData = await response.Content.ReadAsStringAsync();
            var startGameResponse = JsonSerializer.Deserialize<StartGameResponse>(responseData);

            // Initialiser les grilles
            playerGrid = new Grid(10, 10);
            oponnentGrid = new Grid(10, 10);

            if (startGameResponse != null)
            {
                gameId = new Guid(startGameResponse.GameId);
                boats = startGameResponse.PlayerBoats.ToList();
            }

            InitializeBoatPositions();
        }
        else
        {
            Console.WriteLine($"Error calling startGame: {response.StatusCode}");
        }
    }
}