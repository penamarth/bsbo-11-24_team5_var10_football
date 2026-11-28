namespace FootballSystem.Events;
using FootballSystem.UI;


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
        ConsolePanes.log($"Match.create(id={id}, name={name})");
        this.id = id;
        this.name = name;
        this.priceCoefficient = priceCoefficient;
        this.stadiumMap = stadiumMap;
        this.startTimestamp = startTimestamp;
        this.endTimestamp = endTimestamp;
    }

    public int getId()
    {
        ConsolePanes.log("Match.getId()");
        return id;
    }

    public string getName()
    {
        ConsolePanes.log("Match.getName()");
        return name;
    }

    public int getStartTimestamp()
    {
        ConsolePanes.log("Match.getStartTimestamp()");
        return startTimestamp;
    }

    public int getEndTimestamp()
    {
        ConsolePanes.log("Match.getEndTimestamp()");
        return endTimestamp;
    }

    public bool setSeatStatus(int seatId, SeatStatus status)
    {
        ConsolePanes.log($"Match.setSeatStatus(seatId={seatId}, status={status})");
        if (seatStatuses.TryGetValue(seatId, out SeatStatus current) && current == status)
        {
            return false;
        }

        seatStatuses[seatId] = status;
        return true;
    }
}
