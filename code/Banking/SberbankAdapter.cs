namespace FootballSystem.Banking;

using System.Threading.Tasks;

public class SberbankAdapter : IBankAdapter
{
		public async Task<bool> Refund(string receiver, string description, double amount)
		{
				return await SberbankAPI.Transfer("auth-token", receiver, description, amount);
		}

		public async Task<string?> StartPurchase(string description, double amount)
		{
				return await SberbankAPI.CreatePurchase("auth-token", description, amount);
		}

		public async Task<string> ConfirmPurchase(string url)
		{
				return await SberbankAPI.PurchaseStatus(url);
		}
}
