using System;
using System.Collections.Generic;
namespace FootballSystem.Events;

public class EventStorage {
    List<Event> events;

    public List<Event> getEvents() { return this.events; }
    public void addEvent(Event e) {
        this.events.Add(e); 
    }
    public Event findEventById(int eventId) {
        foreach (Event e in events) {
            if (e.getId() == eventId)
                return e;
        }
        return null;
    }
    public void cleanList() {
        foreach (Event e in events) {
            if (e.eventTime.Item2 < (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                events.RemoveAt(events.IndexOf(e));
        }
        int lastId = 1;
        foreach (Event e in events) {
            if (e.getId() > lastId) {
                e.setId(lastId);
                lastId++;
            }
        }
        return;
    }
    public int lastId() {
        int last = 1;
        foreach (Event e in events) {
            last = e.getId();
        }
        return last;
    }

    public void createEvent(Event e) {
        events.Add(e);
    }

    public void createMatch(int eventId, Match match) {
        Event e = findEventById(eventId);
        if (e is not null)
            findEventById(eventId).addMatch(match);
    }
    public bool updateEvent(int eventId, Event newEvent) {
        Event e = findEventById(eventId);
        if (e is not null) {
            e = newEvent;
            return true;
        }
        else
            return false;
    }
    public bool updateMatch(int eventId, int matchId, Match newMatch) {
        Event e = findEventById(eventId);
        if (e is not null) {
            Match m = e.findMatchById(matchId);
            if (m is not null) {
                m = newMatch;
                return true;
            }
            else
                return false;
        }
        else
            return false;
    }
}