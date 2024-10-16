using BattleShip.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BattleShip.Services;

public interface IGameService
{
    Guid? gameId { get; set; }
    List<Boat>? boats { get; set; }
    Grid playerGrid { get; set; }
    Grid opponentGrid { get; set; }
    Task StartGame();
}

public class GameService : IGameService
{
    public Guid? gameId { get; set; }
    public List<Boat>? boats { get; set; }
    public required Grid playerGrid { get; set; }
    public required Grid opponentGrid { get; set; }
    private Dictionary<Position, Boat> boatPositions = new Dictionary<Position, Boat>();
    private string selectedBoatName;
    private int selectedBoatSize;
    private bool isVertical;

    private readonly IGameModalService _modalService;
    private readonly NavigationManager _navManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpService _httpService;

    public GameService(IGameModalService modalService, NavigationManager navManager, ITokenService tokenService, IHttpService httpService)
    {
        _modalService = modalService;
        _navManager = navManager;
        _tokenService = tokenService;
        _httpService = httpService;
    }

    public async Task StartGame()
    {
        var startGameRequest = new StartGameRequest(10, 2);
        var response = await _httpService.SendHttpRequestAsync(HttpMethod.Post, "/startGame", startGameRequest);

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();

            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                string result = doc.RootElement.GetProperty("result").GetString();
                Console.WriteLine(result);
                if (result != null)
                {
                    gameId = Guid.Parse(result);
                    playerGrid = new Grid(10, 10);
                    opponentGrid = new Grid(10, 10);
                    boats = new List<Boat>();
                }
            }
        }
        else
        {
            throw new Exception($"Error calling startGame: {response.StatusCode}");
        }
    }
}
