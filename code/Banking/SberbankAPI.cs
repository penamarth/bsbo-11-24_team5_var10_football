namespace FootballSystem.Banking;

using System;
using System.Threading.Tasks;

static public class SberbankAPI
{
    static public async Task<bool> transfer(string token, string receiver, string description, double amount)
    {
        Console.WriteLine($"[ВЫЗОВ] SberbankAPI.transfer(receiver={receiver}, amount={amount:F2})");
        await Task.Delay(1000);
        return true;
    }

    static public async Task<string?> createPurchase(string token, string description, double amount)
    {
        Console.WriteLine($"[ВЫЗОВ] SberbankAPI.createPurchase(description={description}, amount={amount:F2})");
        await Task.Delay(1000);
        return "https://bank.purchase.link/random-purchase-id";
    }

    static public async Task<string> purchaseStatus(string url)
    {
        Console.WriteLine($"[ВЫЗОВ] SberbankAPI.purchaseStatus(url={url})");
        await Task.Delay(1000);
        return "success";
    }
}
