namespace FootballSystem.Events;

using System;
using System.Collections.Generic;

public class Match : EventComponent
{
    public int id;
    public string name;
    public double priceCoefficient = 1.0;
    public int startTimestamp;
    public int endTimestamp;
    public Dictionary<int, SeatStatus> seatStatuses = new();
    public StadiumMap stadiumMap;

    public Match(int id, string name, double priceCoefficient, StadiumMap stadiumMap, int startTimestamp, int endTimestamp)
    {
        Console.WriteLine($"[ВЫЗОВ] Match.create(id={id}, name={name})");
        this.id = id;
        this.name = name;
        this.priceCoefficient = priceCoefficient;
        this.stadiumMap = stadiumMap;
        this.startTimestamp = startTimestamp;
        this.endTimestamp = endTimestamp;
    }

    public int getId()
    {
        Console.WriteLine("[ВЫЗОВ] Match.getId()");
        return id;
    }

    public string getName()
    {
        Console.WriteLine("[ВЫЗОВ] Match.getName()");
        return name;
    }

    public int getStartTimestamp()
    {
        Console.WriteLine("[ВЫЗОВ] Match.getStartTimestamp()");
        return startTimestamp;
    }

    public int getEndTimestamp()
    {
        Console.WriteLine("[ВЫЗОВ] Match.getEndTimestamp()");
        return endTimestamp;
    }

    public bool setSeatStatus(int seatId, SeatStatus status)
    {
        Console.WriteLine($"[ВЫЗОВ] Match.setSeatStatus(seatId={seatId}, status={status})");
        if (seatStatuses.TryGetValue(seatId, out SeatStatus current) && current == status)
        {
            return false;
        }

        seatStatuses[seatId] = status;
        return true;
    }
}
