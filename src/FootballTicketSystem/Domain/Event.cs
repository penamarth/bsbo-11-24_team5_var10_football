namespace FootballTicketSystem.Domain;

public class Event
{
    private readonly List<Match> _matches = new();

    public Event(int id, string name, DateTimeOffset startTimestamp, DateTimeOffset endTimestamp)
    {
        Id = id;
        Name = name;
        StartTimestamp = startTimestamp;
        EndTimestamp = endTimestamp;
    }

    public int Id { get; internal set; }

    public string Name { get; set; }

    public DateTimeOffset StartTimestamp { get; set; }

    public DateTimeOffset EndTimestamp { get; set; }

    public bool OnSale { get; set; } = true;

    public IReadOnlyList<Match> Matches => _matches;

    public StadiumMap? Map => _matches.FirstOrDefault()?.Map;

    public double PriceCoefficient => _matches.Count > 1 ? 0.85 : 1.0;

    public Match? FindMatchById(int matchId) => _matches.FirstOrDefault(m => m.Id == matchId);

    public bool CreateMatch(Match match)
    {
        if (match.Id != 0 && FindMatchById(match.Id) != null)
        {
            return false;
        }

        _matches.Add(match);
        return true;
    }

    public bool SetSeatStatus(int matchId, int seatId, SeatStatus status)
    {
        var match = FindMatchById(matchId);
        if (match == null)
        {
            return false;
        }

        match.SetSeatStatus(seatId, status);
        return true;
    }

    public Money CalculatePrice(Seat seat) => seat.Price * (decimal)PriceCoefficient;
}
