namespace FootballTicketSystem.Domain;

public sealed class Subscription : Pass
{
    public Subscription(
        IReadOnlyList<int> matchIds,
        int seatId,
        double priceCoefficient,
        EmailAddress email,
        PaymentType paymentMethod,
        string paymentCredentials,
        DateTimeOffset startTimestamp,
        TimeSpan duration)
        : base(seatId, priceCoefficient, email, paymentMethod, paymentCredentials)
    {
        MatchIds = matchIds;
        StartTimestamp = startTimestamp;
        Duration = duration;
    }

    public IReadOnlyList<int> MatchIds { get; }

    public DateTimeOffset StartTimestamp { get; }

    public TimeSpan Duration { get; }

    public IReadOnlyList<int> UsedMatches => Entries.Select(e => e.MatchId).Distinct().ToList();

    public override IReadOnlyList<int> CoveredMatches => MatchIds;
}
