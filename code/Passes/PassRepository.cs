namespace FootballSystem.Passes;
using FootballSystem.UI;


using System;
using System.Collections.Generic;
using System.Linq;

public class PassRepository
{
    private List<Pass> passes = new();

    public void createPass(Pass pass)
    {
        ConsolePanes.log($"PassRepository.createPass(pass={pass.GetType().Name})");
        pass.id = passes.Count + 1;
        passes.Add(pass);
    }

    public bool checkRefundEligibility(int passId)
    {
        ConsolePanes.log($"PassRepository.checkRefundEligibility(passId={passId})");
        Pass? pass = getPassById(passId);
        return pass != null && pass.checkRefundEligibility();
    }

    public bool refund(int passId)
    {
        ConsolePanes.log($"PassRepository.refund(passId={passId})");
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
        ConsolePanes.log($"PassRepository.getPassById(passId={passId})");
        return passes.FirstOrDefault(p => p.id == passId);
    }

    public List<Pass> getPassesWithFilter(Func<Pass, bool> f)
    {
        ConsolePanes.log("PassRepository.getPassesWithFilter()");
        return passes.Where(f).ToList();
    }

    public bool coversAnyMatch(Pass pass, HashSet<int> matchIds)
    {
        ConsolePanes.log($"PassRepository.coversAnyMatch(pass={pass.GetType().Name})");
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
