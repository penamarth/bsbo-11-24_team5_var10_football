namespace FootballSystem.Turnstiles;

using System;
using System.Threading.Tasks;

static public class ExternalTurnstileAPI
{
    static public async Task<bool> sendOpenCommand(int deviceId, string token)
    {
        Console.WriteLine($"[ВЫЗОВ] ExternalTurnstileAPI.sendOpenCommand(deviceId={deviceId})");
        await Task.Delay(300);
        return true;
    }

    static public async Task<bool> sendCloseCommand(int deviceId, string token)
    {
        Console.WriteLine($"[ВЫЗОВ] ExternalTurnstileAPI.sendCloseCommand(deviceId={deviceId})");
        await Task.Delay(300);
        return true;
    }

    static public async Task<string> queryDeviceState(int deviceId)
    {
        Console.WriteLine($"[ВЫЗОВ] ExternalTurnstileAPI.queryDeviceState(deviceId={deviceId})");
        await Task.Delay(300);
        return "waiting";
    }
}
