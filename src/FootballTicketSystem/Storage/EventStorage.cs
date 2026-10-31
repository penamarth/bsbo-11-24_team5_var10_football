using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Storage;

public sealed class EventStorage
{
    private readonly List<Event> _events = new();
    private int _nextEventId = 1;
    private int _nextMatchId = 1;

    public IReadOnlyList<Event> LoadEvents() => _events;

    public Event? FindEventById(int eventId) => _events.FirstOrDefault(e => e.Id == eventId);

    public Event? FindEventByName(string name) => _events.FirstOrDefault(e => e.Name == name);

    public Event? FindEventByMatch(int matchId) => _events.FirstOrDefault(e => e.Matches.Any(m => m.Id == matchId));

    public Match? FindMatchById(int matchId) => _events.SelectMany(e => e.Matches).FirstOrDefault(m => m.Id == matchId);

    public bool CreateEvent(Event e)
    {
        if (e.Id != 0 && FindEventById(e.Id) != null)
        {
            return false;
        }

        if (e.Id == 0)
        {
            e.Id = _nextEventId++;
        }

        foreach (var match in e.Matches)
        {
            if (match.Id == 0)
            {
                match.Id = _nextMatchId++;
            }

            match.EventId = e.Id;
        }

        _events.Add(e);
        return true;
    }

    public bool UpdateEvent(int eventId, Event newEvent)
    {
        var e = FindEventById(eventId);
        if (e == null)
        {
            return false;
        }

        e.Name = newEvent.Name;
        e.StartTimestamp = newEvent.StartTimestamp;
        e.EndTimestamp = newEvent.EndTimestamp;
        e.OnSale = newEvent.OnSale;
        return true;
    }

    public bool UpdateMatch(int matchId, Match newMatch)
    {
        var match = FindMatchById(matchId);
        if (match == null)
        {
            return false;
        }

        match.Name = newMatch.Name;
        match.StadiumName = newMatch.StadiumName;
        match.StartTimestamp = newMatch.StartTimestamp;
        match.EndTimestamp = newMatch.EndTimestamp;
        match.OnSale = newMatch.OnSale;
        return true;
    }

    public IReadOnlyList<Seat> AvailableSeats(Event e)
    {
        if (e.Map == null)
        {
            return Array.Empty<Seat>();
        }

        return e.Map.Seats.Where(s => e.Matches.All(m => m.IsSeatAvailable(s))).ToList();
    }

    public Reservation? ReserveSeat(Event e, int seatId, TimeSpan timeout, DateTimeOffset now)
    {
        if (!e.OnSale || e.Matches.Any(m => !m.OnSale))
        {
            return null;
        }

        var seat = e.Map?.FindSeat(seatId);
        if (seat == null || !e.Matches.All(m => m.IsSeatAvailable(seat)))
        {
            return null;
        }

        foreach (var match in e.Matches)
        {
            match.SetSeatStatus(seatId, SeatStatus.Booked);
        }

        return new Reservation(e.Id, seatId, e.Matches.Select(m => m.Id).ToList(), now + timeout);
    }

    public bool IsReservationActive(Reservation reservation, DateTimeOffset now)
    {
        if (reservation.IsExpired(now))
        {
            return false;
        }

        return reservation.MatchIds.All(id => FindMatchById(id)?.IsBooked(reservation.SeatId) == true);
    }

    public void ReleaseReservation(Reservation reservation)
    {
        foreach (var matchId in reservation.MatchIds)
        {
            FindMatchById(matchId)?.ReleaseSeat(reservation.SeatId);
        }
    }

    public void ConfirmReservation(Reservation reservation)
    {
        foreach (var matchId in reservation.MatchIds)
        {
            FindMatchById(matchId)?.SetSeatStatus(reservation.SeatId, SeatStatus.Sold);
        }

        var seat = FindEventById(reservation.EventId)?.Map?.FindSeat(reservation.SeatId);
        if (seat != null)
        {
            seat.Status = SeatStatus.Sold;
        }
    }

    public bool HasStarted(IReadOnlyList<int> matchIds, DateTimeOffset now) =>
        matchIds.Any(id => FindMatchById(id) is { } match && match.StartTimestamp <= now);
}
