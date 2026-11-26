namespace FootballSystem.Passes;

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
        Console.WriteLine($"[ВЫЗОВ] Subscription.create(matchCount={matchIds.Count})");
        this.matchIds = matchIds;
    }

    public override bool checkAccess(int matchId)
    {
        Console.WriteLine($"[ВЫЗОВ] Subscription.checkAccess(matchId={matchId})");
        return matchIds.Contains(matchId) && !usedMatches.Contains(matchId);
    }

    public override bool markUsed(int matchId)
    {
        Console.WriteLine($"[ВЫЗОВ] Subscription.markUsed(matchId={matchId})");
        if (!checkAccess(matchId))
        {
            return false;
        }

        usedMatches.Add(matchId);
        return true;
    }

    public override bool checkRefundEligibility()
    {
        Console.WriteLine("[ВЫЗОВ] Subscription.checkRefundEligibility()");
        return !isRefunded && usedMatches.Count == 0;
    }
}
