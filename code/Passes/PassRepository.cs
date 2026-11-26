namespace FootballSystem.Passes;

using System;
using System.Collections.Generic;
using System.Linq;

public class PassRepository
{
    private List<Pass> passes = new();

    public void createPass(Pass pass)
    {
        Console.WriteLine($"[ВЫЗОВ] PassRepository.createPass(pass={pass.GetType().Name})");
        pass.id = passes.Count + 1;
        passes.Add(pass);
    }

    public bool checkRefundEligibility(int passId)
    {
        Console.WriteLine($"[ВЫЗОВ] PassRepository.checkRefundEligibility(passId={passId})");
        Pass? pass = getPassById(passId);
        return pass != null && pass.checkRefundEligibility();
    }

    public bool refund(int passId)
    {
        Console.WriteLine($"[ВЫЗОВ] PassRepository.refund(passId={passId})");
        Pass? pass = getPassById(passId);
        if (pass == null || pass.isRefunded)
        {
            return false;
        }

        pass.isRefunded = true;
        return true;
    }

    public Pass? getPassById(int passId)
    {
        Console.WriteLine($"[ВЫЗОВ] PassRepository.getPassById(passId={passId})");
        return passes.FirstOrDefault(p => p.id == passId);
    }

    public List<Pass> getPassesWithFilter(Func<Pass, bool> f)
    {
        Console.WriteLine("[ВЫЗОВ] PassRepository.getPassesWithFilter()");
        return passes.Where(f).ToList();
    }

    public bool coversAnyMatch(Pass pass, HashSet<int> matchIds)
    {
        Console.WriteLine($"[ВЫЗОВ] PassRepository.coversAnyMatch(pass={pass.GetType().Name})");
        switch (pass)
        {
            case Ticket ticket:
                return matchIds.Contains(ticket.matchId);
            case Subscription subscription:
                return subscription.matchIds.Overlaps(matchIds);
            default:
                return false;
        }
    }
}
