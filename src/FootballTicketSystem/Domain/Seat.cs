namespace FootballTicketSystem.Domain;

public class Seat
{
    public Seat(int id, int sector, int row, int column, Money price)
    {
        Id = id;
        Sector = sector;
        Row = row;
        Column = column;
        Price = price;
    }

    public int Id { get; }

    public int Sector { get; }

    public int Row { get; }

    public int Column { get; }

    public Money Price { get; }

    public SeatStatus Status { get; set; } = SeatStatus.Free;

    public string Location => $"сектор {Sector}, ряд {Row}, место {Column}";
}
