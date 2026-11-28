namespace FootballSystem.Turnstiles;
using FootballSystem.UI;


using System;

public class TurnstileAdapter : ITurnstileAdapter
{
    public bool unlock(int turnstileId)
    {
        ConsolePanes.log($"TurnstileAdapter.unlock(turnstileId={turnstileId})");
        return ExternalTurnstileAPI
            .sendOpenCommand(turnstileId, "auth-token")
            .GetAwaiter()
            .GetResult();
    }

    public bool @lock(int turnstileId)
    {
        ConsolePanes.log($"TurnstileAdapter.lock(turnstileId={turnstileId})");
        return ExternalTurnstileAPI
            .sendCloseCommand(turnstileId, "auth-token")
            .GetAwaiter()
            .GetResult();
    }

    public string getStatus(int turnstileId)
    {
        ConsolePanes.log($"TurnstileAdapter.getStatus(turnstileId={turnstileId})");
        return ExternalTurnstileAPI
            .queryDeviceState(turnstileId)
            .GetAwaiter()
            .GetResult();
    }
}
