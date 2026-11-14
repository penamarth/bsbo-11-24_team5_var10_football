namespace FootballSystem.Events;

public class Seat
{
    int id;
    int sector;
    int row;
    int column;
    double price;
    SeatStatus status;

    public Seat(int id, int sector, int row, int column, double price = 1200, SeatStatus status = SeatStatus.Free) {
        this.id = id;
        this.sector = sector;
        this.row = row;
        this.column = column;
        this.price = price;
        this.status = status;
    }

    public int getId()
    {
        return this.id;
    }
    public void setStatus(SeatStatus newStatus)
    {
        this.status = newStatus;
    }
    public SeatStatus getStatus()
    {
        return this.status;
    }
    public void changePrice(double price) {
        this.price = price;
    }
}
