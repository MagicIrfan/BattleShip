namespace BattleShip.Services;

public interface IGameEventService
{
    event Action? OnGameRestarted;
    void RaiseGameRestarted();
}
public class GameEventService : IGameEventService
{
    public event Action? OnGameRestarted;

    public void RaiseGameRestarted()
    {
        OnGameRestarted?.Invoke();
    }
}