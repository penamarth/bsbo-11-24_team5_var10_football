namespace FootballSystem.Events;

using System;

public class Seat
{
    public int id;
    public int sector;
    public int row;
    public int column;
    public double price;

    public Seat(int id, int sector, int row, int column, double price)
    {
        Console.WriteLine($"[ВЫЗОВ] Seat.create(id={id}, price={price:F2})");
        this.id = id;
        this.sector = sector;
        this.row = row;
        this.column = column;
        this.price = price;
    }
}
