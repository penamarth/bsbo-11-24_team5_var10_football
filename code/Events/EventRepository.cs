namespace FootballSystem.Events;
using FootballSystem.UI;


using System;
using System.Collections.Generic;
using System.Linq;

public class EventRepository
{
    private List<Event> events = new();
    private List<StadiumMap> stadiumMaps = new();
    private Dictionary<(int MatchId, int SeatId), int> bookingDeadlines = new();

    public List<Event> getEvents()
    {
        ConsolePanes.log("EventRepository.getEvents()");
        return events;
    }

    public Event? findEventById(int id)
    {
        ConsolePanes.log($"EventRepository.findEventById(id={id})");
        return events.FirstOrDefault(e => e.id == id);
    }

    public Match? findMatchById(int id)
    {
        ConsolePanes.log($"EventRepository.findMatchById(id={id})");
        return events
            .Select(e => e.findMatchById(id))
            .FirstOrDefault(match => match != null);
    }

    public List<StadiumMap> getStadiumMaps()
    {
        ConsolePanes.log("EventRepository.getStadiumMaps()");
        return stadiumMaps;
    }

    public StadiumMap? findStadiumMapById(int id)
    {
        ConsolePanes.log($"EventRepository.findStadiumMapById(id={id})");
        return stadiumMaps.FirstOrDefault(map => map.id == id);
    }

    public void createStadiumMap(StadiumMap map)
    {
        ConsolePanes.log($"EventRepository.createStadiumMap(map={map.name})");
        map.id = stadiumMaps.Select(m => m.id).DefaultIfEmpty(0).Max() + 1;
        stadiumMaps.Add(map);
    }

    public void createEvent(Event e)
    {
        ConsolePanes.log($"EventRepository.createEvent(e={e.getName()})");
        e.id = events.Count + 1;
        events.Add(e);
    }

    public bool createMatch(int eventId, Match match)
    {
        ConsolePanes.log($"EventRepository.createMatch(eventId={eventId}, match={match.getName()})");
        Event? targetEvent = findEventById(eventId);
        if (targetEvent == null)
        {
            return false;
        }

        match.id = events
            .SelectMany(ev => ev.getChildren())
            .OfType<Match>()
            .Select(m => m.id)
            .DefaultIfEmpty(0)
            .Max() + 1;

        targetEvent.addChild(match);
        return true;
    }

    public bool reserveSeat(int matchId, int seatId, int timeoutSeconds = 300)
    {
        ConsolePanes.log($"EventRepository.reserveSeat(matchId={matchId}, seatId={seatId})");
        Match? match = findMatchById(matchId);
        if (match == null)
        {
            return false;
        }

        SeatStatus status = match.seatStatuses.GetValueOrDefault(seatId, SeatStatus.Free);
        if (status == SeatStatus.Booked && checkBooking(matchId, seatId))
        {
            return false;
        }

        if (status != SeatStatus.Free && status != SeatStatus.Booked)
        {
            return false;
        }

        if (!match.setSeatStatus(seatId, SeatStatus.Booked))
        {
            return false;
        }

        bookingDeadlines[(matchId, seatId)] = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + timeoutSeconds;
        return true;
    }

    public bool checkBooking(int matchId, int seatId)
    {
        ConsolePanes.log($"EventRepository.checkBooking(matchId={matchId}, seatId={seatId})");
        Match? match = findMatchById(matchId);
        if (match == null || match.seatStatuses.GetValueOrDefault(seatId, SeatStatus.Free) != SeatStatus.Booked)
        {
            return false;
        }

        if (bookingDeadlines.TryGetValue((matchId, seatId), out int deadline)
            && DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= deadline)
        {
            releaseSeat(matchId, seatId);
            return false;
        }

        return true;
    }

    public bool releaseSeat(int matchId, int seatId)
    {
        ConsolePanes.log($"EventRepository.releaseSeat(matchId={matchId}, seatId={seatId})");
        Match? match = findMatchById(matchId);
        if (match == null || match.seatStatuses.GetValueOrDefault(seatId, SeatStatus.Free) != SeatStatus.Booked)
        {
            return false;
        }

        bookingDeadlines.Remove((matchId, seatId));
        return match.setSeatStatus(seatId, SeatStatus.Free);
    }

    public bool refundSeat(int matchId, int seatId)
    {
        ConsolePanes.log($"EventRepository.refundSeat(matchId={matchId}, seatId={seatId})");
        Match? match = findMatchById(matchId);
        if (match == null || match.seatStatuses.GetValueOrDefault(seatId, SeatStatus.Free) != SeatStatus.Sold)
        {
            return false;
        }

        bookingDeadlines.Remove((matchId, seatId));
        return match.setSeatStatus(seatId, SeatStatus.Free);
    }

    public bool updateEvent(int id, Event newEvent)
    {
        ConsolePanes.log($"EventRepository.updateEvent(id={id}, newEvent={newEvent.getName()})");
        Event? e = findEventById(id);
        if (e == null)
        {
            return false;
        }

        e.id = id;
        e.name = newEvent.getName();
        e.children = newEvent.getChildren();
        e.startTimestamp = newEvent.getStartTimestamp();
        e.endTimestamp = newEvent.getEndTimestamp();

        return true;
    }

    public bool updateMatch(int id, Match newMatch)
    {
        ConsolePanes.log($"EventRepository.updateMatch(id={id}, newMatch={newMatch.getName()})");
        Match? match = findMatchById(id);
        if (match == null)
        {
            return false;
        }

        match.id = id;
        match.name = newMatch.getName();
        match.priceCoefficient = newMatch.priceCoefficient;
        match.stadiumMap = newMatch.stadiumMap;
        match.startTimestamp = newMatch.getStartTimestamp();
        match.endTimestamp = newMatch.getEndTimestamp();
        match.seatStatuses = newMatch.seatStatuses;

        return true;
    }
}
