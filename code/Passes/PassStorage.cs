namespace FootballSystem.Passes;

using FootballSystem.Events;
using System.Collections.Generic;
using System.Linq;

public class PassStorage
{
    private List<Pass> _passes = new();

    public PassStorage() {}

    public void CreatePass(Pass pass)
    {
        pass.ID = _passes.Count + 1;
        _passes.Add(pass);
    }

    public bool CheckRefundEligibility(int passId)
    {
        Pass? pass = GetPassByID(passId);
        
        return pass != null && pass.CheckRefundEligibility();
    }

    public bool Refund(int passId)
    {
        Pass? pass = GetPassByID(passId);
        
        if (pass == null || pass.IsRefunded)
        {
            return false;
        }

        pass.IsRefunded = true; 
        return true;
    }

    public Pass? GetPassByID(int passId) 
    {
        return _passes.FirstOrDefault(p => p.ID == passId);
    }

    public List<Pass> GetPassesForEvent(Event e)
    {
        HashSet<int> matchIds = e.Matches.Select(m => m.ID).ToHashSet();

        return _passes.Where(p => CoversAnyMatch(p, matchIds)).ToList();
    }

    public bool HasSalesForEvent(Event e)
    {
        return GetPassesForEvent(e).Count > 0;
    }

    private static bool CoversAnyMatch(Pass pass, HashSet<int> matchIds)
    {
        switch (pass)
        {
            case Ticket ticket:
                return matchIds.Contains(ticket.MatchID);
            case Subscription subscription:
                return subscription.MatchIDs.Overlaps(matchIds);
            default:
                return false;
        }
    }
}
