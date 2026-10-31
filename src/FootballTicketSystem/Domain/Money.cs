namespace FootballTicketSystem.Domain;

public readonly record struct Money(decimal Amount)
{
    public static readonly Money Zero = new(0m);

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator *(Money money, decimal coefficient) =>
        new(Math.Round(money.Amount * coefficient, 2, MidpointRounding.AwayFromZero));

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public override string ToString() => $"{Amount:0.00} ₽";
}
