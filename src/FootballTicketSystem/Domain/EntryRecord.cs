namespace FootballTicketSystem.Domain;

public sealed class EntryRecord
{
    internal EntryRecord(Pass pass, int matchId, int turnstileId, DateTimeOffset timestamp)
    {
        Pass = pass;
        MatchId = matchId;
        TurnstileId = turnstileId;
        Timestamp = timestamp;
    }

    public Pass Pass { get; }

    public int MatchId { get; }

    public int TurnstileId { get; }

    public DateTimeOffset Timestamp { get; }
}
