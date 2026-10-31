namespace FootballTicketSystem.Domain;

public sealed class Refund
{
    public Refund(int passId, Money amount, PaymentType method)
    {
        PassId = passId;
        Amount = amount;
        Method = method;
    }

    public int PassId { get; }

    public Money Amount { get; }

    public PaymentType Method { get; }

    public bool ReturnedToCard { get; private set; }

    public bool PaidInCash { get; private set; }

    public void MarkReturnedToCard() => ReturnedToCard = true;

    public void MarkPaidInCash() => PaidInCash = true;
}
