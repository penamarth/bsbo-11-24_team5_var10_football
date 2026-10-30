using FootballSystem.Enums;
using FootballSystem.Models;
using FootballSystem.Storages;
using FootballSystem.Banking;

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
