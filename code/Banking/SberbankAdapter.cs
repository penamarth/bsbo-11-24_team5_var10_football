namespace FootballSystem.Banking;
using FootballSystem.UI;


using System;
using System.Threading.Tasks;

public class SberbankAdapter : IBankAdapter
{
    public async Task<bool> refund(string receiver, string description, double amount)
    {
        ConsolePanes.log($"SberbankAdapter.refund(receiver={receiver}, amount={amount:F2})");
        return await SberbankAPI.transfer("auth-token", receiver, description, amount);
    }

    public async Task<string?> startPurchase(string description, double amount)
    {
        ConsolePanes.log($"SberbankAdapter.startPurchase(description={description}, amount={amount:F2})");
        return await SberbankAPI.createPurchase("auth-token", description, amount);
    }

    public async Task<string> confirmPurchase(string url)
    {
        ConsolePanes.log($"SberbankAdapter.confirmPurchase(url={url})");
        return await SberbankAPI.purchaseStatus(url);
    }
}
