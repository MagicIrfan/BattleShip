using Microsoft.AspNetCore.Components;
using BattleShip.Models;
using BattleShip.Components;
namespace BattleShip.Services;

public interface IGameUIService
{
    Task HandleEndGameConditions(AttackResponse attackResponse);
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

    public async Task HandleEndGameConditions(AttackResponse attackResponse)
    {
        if (attackResponse.PlayerIsWinner)
        {
            await ShowVictoryModal("Gagné", "Vous avez gagné la partie");
        }
        else if (attackResponse.AiIsWinner)
        {
            await ShowVictoryModal("Perdu", "Vous avez perdu la partie");
        }
    }

    private async Task ShowVictoryModal(string title, string message)
    {
        var result = await _modalService.ShowModal<GameModal>(title, message);
        if (result == "restart")
        {
            _eventService.RaiseGameRestarted();
        }
        else if (result == "return")
        {
            _navManager.NavigateTo("/");
        }
    }
}
