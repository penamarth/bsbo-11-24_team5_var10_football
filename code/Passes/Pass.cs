namespace FootballSystem.Models;

using FootballSystem.Enums;

public abstract class Pass
{
		public int ID;
		public int SeatID;
		public double Price;
		public string Email;
		public bool IsRefunded;
		public PaymentType? PaymentMethod;
		public string? PaymentCredentials;

		public abstract bool CheckAccess(int matchId);
		public abstract bool MarkUsed(int matchId);
		public abstract bool CheckRefundEligibility();
}
