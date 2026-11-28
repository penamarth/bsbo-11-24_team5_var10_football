namespace FootballSystem.Passes;
using FootballSystem.UI;

using System;

public abstract class Pass
{
    public int id;
    public int seatId;
    public double price;
    public string email = "";
    public bool isRefunded;
    public PaymentType? paymentMethod;
    public string? paymentCredentials;
    public string QRCode = "";

    internal event Action<Pass>? UsageChanged;

    public abstract bool checkAccess(int matchId);
    public abstract bool markUsed(int matchId);
    public abstract bool checkRefundEligibility();

    protected void notifyUsageChanged()
    {
        ConsolePanes.log($"Pass.notifyUsageChanged(passId={id})");
        UsageChanged?.Invoke(this);
    }
}
