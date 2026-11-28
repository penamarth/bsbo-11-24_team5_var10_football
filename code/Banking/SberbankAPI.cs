namespace FootballSystem.Banking;
using FootballSystem.UI;


using System;
using System.Threading.Tasks;

static public class SberbankAPI
{
    static public async Task<bool> transfer(string token, string receiver, string description, double amount)
    {
        ConsolePanes.log($"SberbankAPI.transfer(receiver={receiver}, amount={amount:F2})");
        await Task.Delay(1000);
        return true;
    }

    static public async Task<string?> createPurchase(string token, string description, double amount)
    {
        ConsolePanes.log($"SberbankAPI.createPurchase(description={description}, amount={amount:F2})");
        await Task.Delay(1000);
        return "https://bank.purchase.link/random-purchase-id";
    }

    static public async Task<string> purchaseStatus(string url)
    {
        ConsolePanes.log($"SberbankAPI.purchaseStatus(url={url})");
        await Task.Delay(1000);
        return "success";
    }
}
