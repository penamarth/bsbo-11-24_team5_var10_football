namespace FootballSystem.Models;

using System.Collections.Generic;

public class Subscription : Pass
{
		public HashSet<int> MatchIDs = new();
		public HashSet<int> UsedMatches = new();
		public int StartUnixTimestamp;
		public int EndUnixTimestamp;

		public Subscription(HashSet<int> matchIds) => MatchIDs = matchIds;

		public override bool CheckAccess(int matchId)
		{
				return MatchIDs.Contains(matchId) && !UsedMatches.Contains(matchId);
		}

		public override bool MarkUsed(int matchId)
		{
				if (!CheckAccess(matchId))
				{
						return false;
				}
				UsedMatches.Add(matchId);
				return true;
		}

		public override bool CheckRefundEligibility()
		{
				return !IsRefunded && UsedMatches.Count == 0;
		}
}
