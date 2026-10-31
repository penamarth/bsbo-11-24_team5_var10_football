namespace FootballTicketSystem.Domain;

public abstract class Pass
{
    private readonly List<EntryRecord> _entries = new();

    protected Pass(int seatId, double priceCoefficient, EmailAddress email, PaymentType paymentMethod, string paymentCredentials)
    {
        SeatId = seatId;
        PriceCoefficient = priceCoefficient;
        Email = email;
        PaymentMethod = paymentMethod;
        PaymentCredentials = paymentCredentials;
        QrToken = $"FTS-{Guid.NewGuid():N}";
    }

    public int Id { get; internal set; }

    public int SeatId { get; }

    public double PriceCoefficient { get; }

    public EmailAddress Email { get; set; }

    public PaymentType PaymentMethod { get; }

    public string PaymentCredentials { get; }

    public bool IsRefunded { get; private set; }

    public string QrToken { get; }

    public abstract IReadOnlyList<int> CoveredMatches { get; }

    public IReadOnlyList<EntryRecord> Entries => _entries;

    public Money CalculatePrice(Seat seat) => seat.Price * (decimal)PriceCoefficient;

    public bool CheckAccess(int matchId) =>
        !IsRefunded && CoveredMatches.Contains(matchId) && !IsUsedFor(matchId);

    public bool IsUsedFor(int matchId) => _entries.Any(e => e.MatchId == matchId);

    public EntryRecord MarkUsed(int matchId, int turnstileId, DateTimeOffset timestamp)
    {
        if (!CheckAccess(matchId))
        {
            throw new InvalidOperationException($"Билет/абонемент #{Id} нельзя использовать на матче #{matchId}");
        }

        var record = new EntryRecord(this, matchId, turnstileId, timestamp);
        _entries.Add(record);
        return record;
    }

    public void Refund() => IsRefunded = true;
}
