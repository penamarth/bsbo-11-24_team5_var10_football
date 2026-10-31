namespace FootballSystem.Models;

public class Report
{
		public int EventID;
		public double TotalRevenue;
		public int TicketsSold;
		public int AttendanceCount;
		public int UpdatedAtUnix;

		public Report()
		{
		}

		public void CalculateMetrics()
		{
		}
}
