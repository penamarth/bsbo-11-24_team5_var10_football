using FootballSystem.Application;
using FootballSystem.Events;
using FootballSystem.Banking;
using FootballSystem.Passes;
using FootballSystem.Turnstiles;

using System;
using System.Threading.Tasks;

namespace FootballSystem;

using FootballSystem.UI;


class Program
{
    static async Task Main(string[] args)
    {
        ConsolePanes.init();
        ConsolePanes.log("Program.Main()");

        EventRepository eventRepo = new();
        PassRepository passRepo = new();
        SberbankAdapter bank = new();
        TurnstileAdapter turnstiles = new();

        FTS fts = new(passRepo, eventRepo, bank, turnstiles);
        await fts.run();
    }
}
