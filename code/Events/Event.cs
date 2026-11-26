namespace FootballSystem.Events;

using System;
using System.Collections.Generic;
using System.Linq;

public class Event : EventComponent
{
    public int id;
    public string name;
    public int startTimestamp;
    public int endTimestamp;
    public List<EventComponent> children = new();

    public Event(int id, string name, int startTimestamp, int endTimestamp)
    {
        Console.WriteLine($"[ВЫЗОВ] Event.create(id={id}, name={name})");
        this.id = id;
        this.name = name;
        this.startTimestamp = startTimestamp;
        this.endTimestamp = endTimestamp;
    }

    public int getId()
    {
        Console.WriteLine("[ВЫЗОВ] Event.getId()");
        return id;
    }

    public string getName()
    {
        Console.WriteLine("[ВЫЗОВ] Event.getName()");
        return name;
    }

    public int getStartTimestamp()
    {
        Console.WriteLine("[ВЫЗОВ] Event.getStartTimestamp()");
        return startTimestamp;
    }

    public int getEndTimestamp()
    {
        Console.WriteLine("[ВЫЗОВ] Event.getEndTimestamp()");
        return endTimestamp;
    }

    public void addChild(EventComponent c)
    {
        Console.WriteLine($"[ВЫЗОВ] Event.addChild(c={c.getName()})");
        children.Add(c);
    }

    public bool removeChild(int id)
    {
        Console.WriteLine($"[ВЫЗОВ] Event.removeChild(id={id})");
        EventComponent? child = children.FirstOrDefault(c => c.getId() == id);
        return child != null && children.Remove(child);
    }

    public List<EventComponent> getChildren()
    {
        Console.WriteLine("[ВЫЗОВ] Event.getChildren()");
        return children;
    }

    public Match? findMatchById(int matchId)
    {
        Console.WriteLine($"[ВЫЗОВ] Event.findMatchById(matchId={matchId})");

        foreach (EventComponent child in children)
        {
            if (child is Match match && match.getId() == matchId)
            {
                return match;
            }

            if (child is Event nestedEvent)
            {
                Match? found = nestedEvent.findMatchById(matchId);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    public bool setSeatStatus(int matchId, int seatId, SeatStatus status)
    {
        Console.WriteLine($"[ВЫЗОВ] Event.setSeatStatus(matchId={matchId}, seatId={seatId}, status={status})");
        Match? match = findMatchById(matchId);
        return match != null && match.setSeatStatus(seatId, status);
    }
}
