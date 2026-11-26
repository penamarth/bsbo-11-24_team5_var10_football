namespace FootballSystem.Passes;

using System;

public class Ticket : Pass
{
    public int matchId;
    public bool used = false;

    public Ticket(int matchId, double price)
    {
        Console.WriteLine($"[ВЫЗОВ] Ticket.create(matchId={matchId}, price={price:F2})");
        this.matchId = matchId;
        this.price = price;
    }

    public override bool checkAccess(int matchId)
    {
        Console.WriteLine($"[ВЫЗОВ] Ticket.checkAccess(matchId={matchId})");
        return !used && matchId == this.matchId;
    }

    public override bool markUsed(int matchId)
    {
        Console.WriteLine($"[ВЫЗОВ] Ticket.markUsed(matchId={matchId})");
        if (matchId != this.matchId || used)
        {
            return false;
        }

        used = true;
        return true;
    }

    public override bool checkRefundEligibility()
    {
        Console.WriteLine("[ВЫЗОВ] Ticket.checkRefundEligibility()");
        return !isRefunded && !used;
    }
}
