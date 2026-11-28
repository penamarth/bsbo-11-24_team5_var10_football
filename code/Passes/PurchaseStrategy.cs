namespace FootballSystem.Passes;

using FootballSystem.Events;

using System.Collections.Generic;

public interface PurchaseStrategy
{
    bool needsMatchSelection();
    List<Match> getMatches(Event ev, Match? selectedMatch);
    string getTitle(Event ev, Match match);
    double calculatePrice(Seat seat, List<Match> matches);
    string getDescription(Event ev, Match match, Seat seat);
    Pass createPass(Event ev, List<Match> matches, Seat seat, double price);
}
