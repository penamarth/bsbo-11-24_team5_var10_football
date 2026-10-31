using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Bank;

public sealed class BankAdapter : IBankAdapter
{
    private readonly SberbankAPI _sberbank;

    public BankAdapter(SberbankAPI sberbank) => _sberbank = sberbank;

    public string StartPurchase(Money amount) => _sberbank.RegisterOrder(amount);

    public bool ChargeCard(string orderToken) => _sberbank.ChargeCard(orderToken);

    public bool StartRefund(string credentials, Money amount) => _sberbank.RefundTransaction(credentials);
}
