namespace FootballSystem.Passes;

using System.Collections.Generic;
using System.Linq;

public class PassStorage
{
		private List<Pass> _passes = new();

		public event Action<Pass>? PassChanged;

		public PassStorage() {}

		public void CreatePass(Pass pass)
		{
				pass.ID = _passes.Count + 1;
				_passes.Add(pass);
				pass.UsageChanged += NotifyPassChanged;
				NotifyPassChanged(pass);
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
				NotifyPassChanged(pass);
				return true;
		}

		private void NotifyPassChanged(Pass pass) => PassChanged?.Invoke(pass);

		public Pass? GetPassByID(int passId) 
		{
				return _passes.FirstOrDefault(p => p.ID == passId);
		}

		public List<Pass> GetPassesWithFilter(Func<Pass, bool> f) {
				return _passes.Where(f).ToList();
		}

		public bool CoversAnyMatch(Pass pass, HashSet<int> matchIds)
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
