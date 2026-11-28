namespace FootballSystem.Events;
using FootballSystem.UI;


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
        ConsolePanes.log($"Event.create(id={id}, name={name})");
        this.id = id;
        this.name = name;
        this.startTimestamp = startTimestamp;
        this.endTimestamp = endTimestamp;
    }

    public int getId()
    {
        ConsolePanes.log("Event.getId()");
        return id;
    }

    public string getName()
    {
        ConsolePanes.log("Event.getName()");
        return name;
    }

    public int getStartTimestamp()
    {
        ConsolePanes.log("Event.getStartTimestamp()");
        return startTimestamp;
    }

    public int getEndTimestamp()
    {
        ConsolePanes.log("Event.getEndTimestamp()");
        return endTimestamp;
    }

    public void addChild(EventComponent c)
    {
        ConsolePanes.log($"Event.addChild(c={c.getName()})");
        children.Add(c);
    }

    public bool removeChild(int id)
    {
        ConsolePanes.log($"Event.removeChild(id={id})");
        EventComponent? child = children.FirstOrDefault(c => c.getId() == id);
        return child != null && children.Remove(child);
    }

    public List<EventComponent> getChildren()
    {
        ConsolePanes.log("Event.getChildren()");
        return children;
    }

    public Match? findMatchById(int matchId)
    {
        ConsolePanes.log($"Event.findMatchById(matchId={matchId})");

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
        ConsolePanes.log($"Event.setSeatStatus(matchId={matchId}, seatId={seatId}, status={status})");
        Match? match = findMatchById(matchId);
        return match != null && match.setSeatStatus(seatId, status);
    }
}
