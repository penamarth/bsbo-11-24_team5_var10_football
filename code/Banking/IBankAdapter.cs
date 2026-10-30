namespace FootballSystem.Banking;

using System.Threading.Tasks;

public interface IBankAdapter
{
		public Task<bool> Refund(string receiver, string description, double amount);
		public Task<string?> StartPurchase(string description, double amount);
		public Task<string> ConfirmPurchase(string url);
}
