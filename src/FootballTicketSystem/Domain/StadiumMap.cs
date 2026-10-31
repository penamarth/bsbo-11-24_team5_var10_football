namespace FootballTicketSystem.Domain;

public class StadiumMap
{
    private readonly List<Seat> _seats = new();
    private readonly List<int> _turnstiles = new();

    public StadiumMap(string name, string layoutImage = "standard_layout.png")
    {
        Name = name;
        LayoutImage = layoutImage;
    }

    public string Name { get; }

    public string LayoutImage { get; set; }

    public IReadOnlyList<Seat> Seats => _seats;

    public IReadOnlyList<int> Turnstiles => _turnstiles;

    public int Capacity => _seats.Count;

    public void AddSeat(Seat seat) => _seats.Add(seat);

    public void AddTurnstile(int turnstileId) => _turnstiles.Add(turnstileId);

    public Seat? FindSeat(int seatId) => _seats.FirstOrDefault(s => s.Id == seatId);

    public IReadOnlyDictionary<SeatStatus, int> GetSeatStatistics() =>
        _seats.GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

    public static StadiumMap CreateStandard(string name, int sectors = 2, int rows = 3, int seatsPerRow = 5, decimal basePrice = 1000m)
    {
        var map = new StadiumMap(name);
        var seatId = 1;

        for (var sector = 1; sector <= sectors; sector++)
        {
            for (var row = 1; row <= rows; row++)
            {
                for (var column = 1; column <= seatsPerRow; column++)
                {
                    var price = basePrice * (1m + (sector - 1) * 0.25m + (row - 1) * 0.1m);
                    map.AddSeat(new Seat(seatId++, sector, row, column, new Money(price)));
                }
            }
        }

        map.AddTurnstile(1);
        map.AddTurnstile(2);
        return map;
    }
}
