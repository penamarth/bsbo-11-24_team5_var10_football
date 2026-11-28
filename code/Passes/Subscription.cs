namespace FootballSystem.Passes;
using FootballSystem.UI;


using System;
using System.Collections.Generic;

public class Subscription : Pass
{
    public HashSet<int> matchIds = new();
    public HashSet<int> usedMatches = new();
    public int startTimestamp;
    public int endTimestamp;

    public Subscription(HashSet<int> matchIds)
    {
        ConsolePanes.log($"Subscription.create(matchCount={matchIds.Count})");
        this.matchIds = matchIds;
    }

    public override bool checkAccess(int matchId)
    {
        ConsolePanes.log($"Subscription.checkAccess(matchId={matchId})");
        return matchIds.Contains(matchId) && !usedMatches.Contains(matchId);
    }

    public override bool markUsed(int matchId)
    {
        ConsolePanes.log($"Subscription.markUsed(matchId={matchId})");
        if (!checkAccess(matchId))
        {
            return false;
        }

        usedMatches.Add(matchId);
        notifyUsageChanged();
        return true;
    }

    public override bool checkRefundEligibility()
    {
        ConsolePanes.log("Subscription.checkRefundEligibility()");
        return !isRefunded && usedMatches.Count == 0;
    }
}
