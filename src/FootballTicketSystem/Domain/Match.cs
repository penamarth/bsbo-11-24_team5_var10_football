namespace FootballTicketSystem.Domain;

public class Match
{
    private readonly Dictionary<int, SeatStatus> _seatStatuses = new();

    public Match(int id, int eventId, string name, string stadiumName, DateTimeOffset startTimestamp, DateTimeOffset endTimestamp)
    {
        Id = id;
        EventId = eventId;
        Name = name;
        StadiumName = stadiumName;
        StartTimestamp = startTimestamp;
        EndTimestamp = endTimestamp;
    }

    public int Id { get; set; }

    public int EventId { get; set; }

    public string Name { get; set; }

    public string StadiumName { get; set; }

    public DateTimeOffset StartTimestamp { get; set; }

    public DateTimeOffset EndTimestamp { get; set; }

    public bool OnSale { get; set; } = true;

    public StadiumMap? Map { get; set; }

    public IReadOnlyDictionary<int, SeatStatus> TakenSeats => _seatStatuses;

    public bool IsSeatAvailable(Seat seat) =>
        seat.Status == SeatStatus.Free && (!_seatStatuses.TryGetValue(seat.Id, out var status) || status == SeatStatus.Free);

    public bool IsBooked(int seatId) =>
        _seatStatuses.TryGetValue(seatId, out var status) && status == SeatStatus.Booked;

    public void SetSeatStatus(int seatId, SeatStatus status) => _seatStatuses[seatId] = status;

    public void ReleaseSeat(int seatId) => _seatStatuses.Remove(seatId);

    public bool AcceptsEntries(DateTimeOffset now) => now >= StartTimestamp.AddHours(-2) && now <= EndTimestamp;
}
