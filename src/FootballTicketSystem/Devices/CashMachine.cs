using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Devices;

public sealed class CashMachine
{
    public Money Till { get; private set; } = Money.Zero;

    public bool TryAcceptPayment(Money price, Money tendered, out Money change)
    {
        change = Money.Zero;
        if (tendered < price)
        {
            return false;
        }

        Till += price;
        change = tendered - price;
        return true;
    }

    public void PayOut(Money amount) => Till -= amount;
}
