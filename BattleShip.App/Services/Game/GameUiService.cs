using Microsoft.AspNetCore.Components;
using BattleShip.Models;
using BattleShip.Components;
namespace BattleShip.Services.Game;

public interface IGameUIService
{
    Task<string?> HandleEndGameConditions(AttackModel.AttackResponse attackResponse);
    void NavigateTo(string url);
}

public class GameUIService : IGameUIService
{
    private readonly IGameModalService _modalService;
    private readonly NavigationManager _navManager;
    private readonly IGameEventService _eventService;

    public GameUIService(IGameModalService modalService, NavigationManager navManager, IGameEventService eventService)
    {
        _modalService = modalService;
        _navManager = navManager;
        _eventService = eventService;
    }

    public async Task<string?> HandleEndGameConditions(AttackModel.AttackResponse attackResponse)
    {
        if (attackResponse.PlayerIsWinner)
        {
            return await _modalService.ShowModal<GameModal>("Gagné", "Vous avez gagné la partie");
        }
        else if (attackResponse.AiIsWinner ?? false)
        {
            return await _modalService.ShowModal<GameModal>("Perdu", "Vous avez perdu la partie");
        }
        return null;
    }

    public void NavigateTo(string url)
    {
        _navManager.NavigateTo(url);
    }
}
