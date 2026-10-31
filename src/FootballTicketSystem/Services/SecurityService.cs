namespace FootballTicketSystem.Services;

public sealed class SecurityService
{
    private readonly List<string> _occupiedSeats = new();
    private readonly List<string> _alerts = new();

    public IReadOnlyList<string> OccupiedSeats => _occupiedSeats;

    public IReadOnlyList<string> Alerts => _alerts;

    public void NotifySeatOccupied(string seatLocation, int matchId) =>
        _occupiedSeats.Add($"матч #{matchId}: {seatLocation}");

    public void NotifyDeniedEntry(string reason) => _alerts.Add(reason);
}
