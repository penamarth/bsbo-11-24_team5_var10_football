namespace FootballSystem.Passes;

public abstract class Pass
{
		internal event Action<Pass>? UsageChanged;

		public int ID;
		public int SeatID;
		public double Price;
		public string Email = "";
		public bool IsRefunded;
		public PaymentType? PaymentMethod;
		public string? PaymentCredentials;
		public string QRCode = "";

		public abstract bool CheckAccess(int matchId);
		public abstract bool MarkUsed(int matchId);
		public abstract bool CheckRefundEligibility();

		protected void NotifyUsageChanged() => UsageChanged?.Invoke(this);
}
