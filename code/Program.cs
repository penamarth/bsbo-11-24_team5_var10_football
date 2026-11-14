using FootballSystem.Application;
using FootballSystem.Events;
using FootballSystem.Banking;
using FootballSystem.Passes;

using System;
using System.Threading.Tasks;

namespace FootballSystem;

class Program
{
    static async Task Main(string[] args)
    {
		EventStorage estorage = new();
		PassStorage pstorage = new();
		SberbankAdapter bank = new();
		FTS fts = new(pstorage, estorage, bank);
		await fts.Run();
    }
}
