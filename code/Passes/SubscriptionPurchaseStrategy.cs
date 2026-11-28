namespace FootballSystem.Passes;

using FootballSystem.Events;
using FootballSystem.UI;

using System;
using System.Collections.Generic;
using System.Linq;

public class SubscriptionPurchaseStrategy : PurchaseStrategy
{
    public bool needsMatchSelection()
    {
        ConsolePanes.log("SubscriptionPurchaseStrategy.needsMatchSelection()");
        return false;
    }

    public List<Match> getMatches(Event ev, Match? selectedMatch)
    {
        ConsolePanes.log($"SubscriptionPurchaseStrategy.getMatches(eventId={ev.getId()})");
        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return ev.getChildren().OfType<Match>()
            .Where(m => m.getEndTimestamp() > now)
            .ToList();
    }

    public string getTitle(Event ev, Match match)
    {
        ConsolePanes.log($"SubscriptionPurchaseStrategy.getTitle(eventId={ev.getId()})");
        return $"subscription for {ev.getName()}";
    }

    public double calculatePrice(Seat seat, List<Match> matches)
    {
        ConsolePanes.log($"SubscriptionPurchaseStrategy.calculatePrice(seatId={seat.id})");
        return matches.Sum(m => seat.price * m.priceCoefficient);
    }

    public string getDescription(Event ev, Match match, Seat seat)
    {
        ConsolePanes.log($"SubscriptionPurchaseStrategy.getDescription(eventId={ev.getId()})");
        return $"FTS subscription: {ev.getName()}, seat {seat.id}";
    }

    public Pass createPass(Event ev, List<Match> matches, Seat seat, double price)
    {
        ConsolePanes.log($"SubscriptionPurchaseStrategy.createPass(eventId={ev.getId()})");
        Subscription subscription = new(matches.Select(m => m.getId()).ToHashSet());
        subscription.startTimestamp = ev.getStartTimestamp();
        subscription.endTimestamp = ev.getEndTimestamp();
        return subscription;
    }
}
