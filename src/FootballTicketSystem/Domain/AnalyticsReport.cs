namespace FootballTicketSystem.Domain;

public sealed class AnalyticsReport
{
    private readonly StadiumMap _map;
    private readonly IReadOnlyList<Pass> _sales;

    public AnalyticsReport(Event currentEvent, StadiumMap map, IReadOnlyList<Pass> sales, DateTimeOffset updatedAt)
    {
        CurrentEvent = currentEvent;
        _map = map;
        _sales = sales;
        UpdatedAt = updatedAt;
    }

    public Event CurrentEvent { get; }

    public int EventId => CurrentEvent.Id;

    public string EventName => CurrentEvent.Name;

    public Money Revenue { get; private set; } = Money.Zero;

    public int TicketsSold { get; private set; }

    public int AttendanceCount { get; private set; }

    public double Occupancy { get; private set; }

    public DateTimeOffset UpdatedAt { get; }

    public bool IsPreliminary { get; private set; }

    public void CalculateMetrics()
    {
        Revenue = Money.Zero;
        TicketsSold = 0;
        IsPreliminary = false;

        foreach (var pass in _sales)
        {
            if (pass.IsRefunded)
            {
                continue;
            }

            var seat = _map.FindSeat(pass.SeatId);
            if (seat == null)
            {
                IsPreliminary = true;
                continue;
            }

            Revenue += pass.CalculatePrice(seat);
            TicketsSold++;
        }

        var eventMatchIds = CurrentEvent.Matches.Select(m => m.Id).ToHashSet();
        AttendanceCount = _sales.Count(p => p.Entries.Any(e => eventMatchIds.Contains(e.MatchId)));

        var statistics = _map.GetSeatStatistics();
        Occupancy = _map.Capacity == 0 ? 0 : (double)statistics.GetValueOrDefault(SeatStatus.Sold) / _map.Capacity;
    }

    public string ToCsv() =>
        "мероприятие;выручка;продано;посещаемость;заполняемость\n" +
        $"{EventName};{Revenue.Amount:0.00};{TicketsSold};{AttendanceCount};{Occupancy:0.00}";

    public override string ToString() =>
        $"Отчёт по мероприятию «{EventName}»: выручка {Revenue}, продано {TicketsSold}, " +
        $"посещаемость {AttendanceCount}, заполняемость {Occupancy:P0}" +
        (IsPreliminary ? " (предварительный)" : string.Empty);
}
