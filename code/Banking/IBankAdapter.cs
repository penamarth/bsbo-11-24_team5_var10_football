namespace FootballSystem.Banking;

using System.Threading.Tasks;

public interface IBankAdapter
{
    Task<bool> refund(string receiver, string description, double amount);
    Task<string?> startPurchase(string description, double amount);
    Task<string> confirmPurchase(string url);
}
