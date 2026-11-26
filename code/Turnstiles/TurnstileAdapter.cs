namespace FootballSystem.Turnstiles;

using System;

public class TurnstileAdapter : ITurnstileAdapter
{
    public bool unlock(int turnstileId)
    {
        Console.WriteLine($"[ВЫЗОВ] TurnstileAdapter.unlock(turnstileId={turnstileId})");
        return ExternalTurnstileAPI
            .sendOpenCommand(turnstileId, "auth-token")
            .GetAwaiter()
            .GetResult();
    }

    public bool @lock(int turnstileId)
    {
        Console.WriteLine($"[ВЫЗОВ] TurnstileAdapter.lock(turnstileId={turnstileId})");
        return ExternalTurnstileAPI
            .sendCloseCommand(turnstileId, "auth-token")
            .GetAwaiter()
            .GetResult();
    }

    public string getStatus(int turnstileId)
    {
        Console.WriteLine($"[ВЫЗОВ] TurnstileAdapter.getStatus(turnstileId={turnstileId})");
        return ExternalTurnstileAPI
            .queryDeviceState(turnstileId)
            .GetAwaiter()
            .GetResult();
    }
}
