namespace FootballTicketSystem.Domain;

public sealed class Reservation
{
    public Reservation(int eventId, int seatId, IReadOnlyList<int> matchIds, DateTimeOffset expiresAt)
    {
        EventId = eventId;
        SeatId = seatId;
        MatchIds = matchIds;
        ExpiresAt = expiresAt;
    }

    public int EventId { get; }

    public int SeatId { get; }

    public IReadOnlyList<int> MatchIds { get; }

    public DateTimeOffset ExpiresAt { get; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
