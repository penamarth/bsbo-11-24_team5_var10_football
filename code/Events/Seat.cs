namespace FootballSystem.Events;

public class Seat
{
    int id;
    int sector;
    int row;
    int column;
    double price;
    SeatStatus status;

    Seat(int id, int sector, int row, int column, double price = 1200, SeatStatus status = SeatStatus.Free) {
        this->id = id;
        this->sector = sector;
        this->row = row;
        this->column = column;
        this->price = price;
        this->status = status;
    }

    public int getId()
    {
        return this->id;
    }
    public bool setStatus(SeatStatus newStatus)
    {
        this->status = newStatus;
    }
    public SeatStatus getStatus()
    {
        return this->status;
    }
    public bool changePrice(double price) {
        return this->price = price;
    }
}
