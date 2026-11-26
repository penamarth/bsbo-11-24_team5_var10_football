namespace FootballSystem.Passes;

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

    public abstract bool checkAccess(int matchId);
    public abstract bool markUsed(int matchId);
    public abstract bool checkRefundEligibility();
}
