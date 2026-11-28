namespace FootballSystem.Turnstiles;
using FootballSystem.UI;


using System;
using System.Threading.Tasks;

static public class ExternalTurnstileAPI
{
    static public async Task<bool> sendOpenCommand(int deviceId, string token)
    {
        ConsolePanes.log($"ExternalTurnstileAPI.sendOpenCommand(deviceId={deviceId})");
        await Task.Delay(300);
        return true;
    }

    static public async Task<bool> sendCloseCommand(int deviceId, string token)
    {
        ConsolePanes.log($"ExternalTurnstileAPI.sendCloseCommand(deviceId={deviceId})");
        await Task.Delay(300);
        return true;
    }

    static public async Task<string> queryDeviceState(int deviceId)
    {
        ConsolePanes.log($"ExternalTurnstileAPI.queryDeviceState(deviceId={deviceId})");
        await Task.Delay(300);
        return "waiting";
    }
}
