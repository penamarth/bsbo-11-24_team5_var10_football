namespace FootballSystem.Models;

public class Ticket : Pass
{
    public int MatchID;
    public bool Used = false;
    
	public Ticket(int matchId, double price) {
			MatchID = matchId;
			Price = price;
	}

    public override bool CheckAccess(int matchId) 
    {
        return !Used && matchId == MatchID;
    }

    public override bool MarkUsed(int matchId) 
    {
        if (matchId != MatchID || Used) 
        {
            return false;
        }
        Used = true;
        return true;
    }

    public override bool CheckRefundEligibility() 
    {
        return !IsRefunded && !Used;
    }
}
