namespace FootballTicketSystem.Domain;

public sealed class Ticket : Pass
{
    public Ticket(int matchId, int seatId, double priceCoefficient, EmailAddress email, PaymentType paymentMethod, string paymentCredentials)
        : base(seatId, priceCoefficient, email, paymentMethod, paymentCredentials)
    {
        MatchId = matchId;
    }

    public int MatchId { get; }

    public bool Used => Entries.Count > 0;

    public override IReadOnlyList<int> CoveredMatches => new[] { MatchId };
}
