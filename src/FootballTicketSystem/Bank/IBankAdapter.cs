using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Bank;

public interface IBankAdapter
{
    string StartPurchase(Money amount);

    bool ChargeCard(string orderToken);

    bool StartRefund(string credentials, Money amount);
}
