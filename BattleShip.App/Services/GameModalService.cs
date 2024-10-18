using BattleShip.Components;
using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace BattleShip.Services;

public interface IGameModalService
{
    Task<string?> ShowModal<T>(string title, string message) where T : ComponentBase;
}

public class GameModalService : IGameModalService
{
    private readonly IModalService _modalService;

    public GameModalService(IModalService modalService)
    {
        _modalService = modalService;
    }

    public async Task<string?> ShowModal<T>(string title, string message) where T : ComponentBase
    {
        var parameters = new ModalParameters();
        parameters.Add("Title", title);
        parameters.Add("Message", message);

        var options = new ModalOptions
        {
            HideCloseButton = true,
            DisableBackgroundCancel = true
        };


        var modal = _modalService.Show<T>(parameters, options);
        var result = await modal.Result;

        return result?.Data?.ToString();
    }
}
