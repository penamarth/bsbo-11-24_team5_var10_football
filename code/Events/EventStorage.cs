using System;
using System.Collections.Generic;
namespace FootballSystem.Events;

public class EventStorage {
    List<Event> events;

    public List<Event> getEvents() { return this->events; }
    public bool addEvent(Event e) {
        return this->events.Add(e); 
    }
    public Event findEventById(int eventId) {
        foreach (Event e in events) {
            if (e.getId() == eventId)
                return e;
        }
    }
    public void cleanList() {
        foreach (Event e in events) {
            if (e.eventTime.Item2 < (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                events.RemoveAt(events.IndexOf(e));
        }
        int lastId = 1;
        foreach (Event e in events) {
            if (e.getId > lastId) {
                e.setId(lastId);
                lastId++;
            }
        }
        return;
    }
    public int lastId() {
        int last;
        foreach (Event e in events) {
            last = e.getId();
        }
        return last;
    }

    public bool createEvent(Event e) {
        return events.Add(e);
    }

    public bool createMatch(int eventId, Match match) {
        if (findEventById(eventId))
            return findEventById(eventId).addMatch(match);
        else
            return false;
    }
    public bool updateEvent(int eventId, Event newEvent) {
        return findEventById(eventId) = newEvent;
    }
    public bool updateMatch(int eventId, int matchId, Match newMatch) {
        return findEventById(eventId).findMatchById(matchId) = newMatch;
    }
}