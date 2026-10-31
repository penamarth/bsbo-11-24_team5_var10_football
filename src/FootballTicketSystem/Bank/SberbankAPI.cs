using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Bank;

public sealed class SberbankAPI
{
    private readonly List<string> _registeredOrders = new();
    private readonly List<string> _chargedOrders = new();

    public bool IsAvailable { get; set; } = true;

    public IReadOnlyList<string> RegisteredOrders => _registeredOrders;

    public IReadOnlyList<string> ChargedOrders => _chargedOrders;

    public string RegisterOrder(Money amount)
    {
        if (!IsAvailable)
        {
            return string.Empty;
        }

        var order = $"SBP-{Guid.NewGuid():N}";
        _registeredOrders.Add(order);
        return order;
    }

    public bool ChargeCard(string orderToken)
    {
        if (!IsAvailable || !_registeredOrders.Remove(orderToken))
        {
            return false;
        }

        _chargedOrders.Add(orderToken);
        return true;
    }

    public bool RefundTransaction(string credentials) => IsAvailable && _chargedOrders.Contains(credentials);
}
