namespace FootballSystem.Banking;

using System.Threading.Tasks;

static public class SberbankAPI
{
		static public async Task<bool> Transfer(string token, string receiver, string description, double amount)
		{
				await Task.Delay(1000);
				return true;
		}
		
		static public async Task<string?> CreatePurchase(string token, string description, double amount)
		{
				await Task.Delay(1000);
				return "https://bank.purchase.link/random-purchase-id";
		}

		static public async Task<string?> PurchaseStatus(string url)
		{
				await Task.Delay(1000);
				return "success";
		}
}
