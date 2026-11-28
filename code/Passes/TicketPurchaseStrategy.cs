namespace FootballSystem.Passes;

using FootballSystem.Events;
using FootballSystem.UI;

using System;
using System.Collections.Generic;
using System.Linq;

public class TicketPurchaseStrategy : PurchaseStrategy
{
    public bool needsMatchSelection()
    {
        ConsolePanes.log("TicketPurchaseStrategy.needsMatchSelection()");
        return true;
    }

    public List<Match> getMatches(Event ev, Match? selectedMatch)
    {
        ConsolePanes.log($"TicketPurchaseStrategy.getMatches(eventId={ev.getId()})");
        return new List<Match> { selectedMatch! };
    }

    public string getTitle(Event ev, Match match)
    {
        ConsolePanes.log($"TicketPurchaseStrategy.getTitle(matchId={match.getId()})");
        return match.getName();
    }

    public double calculatePrice(Seat seat, List<Match> matches)
    {
        ConsolePanes.log($"TicketPurchaseStrategy.calculatePrice(seatId={seat.id})");
        return matches.Sum(m => seat.price * m.priceCoefficient);
    }

    public string getDescription(Event ev, Match match, Seat seat)
    {
        ConsolePanes.log($"TicketPurchaseStrategy.getDescription(matchId={match.getId()})");
        return $"FTS ticket: {ev.getName()} / {match.getName()}, seat {seat.id}";
    }

    public Pass createPass(Event ev, List<Match> matches, Seat seat, double price)
    {
        ConsolePanes.log($"TicketPurchaseStrategy.createPass(matchId={matches.First().getId()})");
        return new Ticket(matches.First().getId(), price);
    }
}
