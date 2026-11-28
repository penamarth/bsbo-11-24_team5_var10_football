namespace FootballSystem.Events;
using FootballSystem.UI;


using System;
using System.Collections.Generic;
using System.Linq;

public class StadiumMap : IPrototype<StadiumMap>
{
    public int id;
    public string name = "";
    public string layoutImage = "";
    public List<Seat> seats = new();
    public List<int> turnstiles = new();

    public StadiumMap(int id = 0)
    {
        ConsolePanes.log($"StadiumMap.create(id={id})");
        this.id = id;
    }

    public StadiumMap clone()
    {
        ConsolePanes.log("StadiumMap.clone()");
        return new StadiumMap
        {
            id = id,
            name = name,
            layoutImage = layoutImage,
            seats = seats.Select(s => new Seat(s.id, s.sector, s.row, s.column, s.price)).ToList(),
            turnstiles = new List<int>(turnstiles)
        };
    }
}
