namespace FootballSystem.Models;

using System;
using System.Collections.Generic;
using System.Globalization;

public class Report
{
		public int EventID;
		public double TotalRevenue;
		public int TicketsSold;
		public int AttendanceCount;
		public int UpdatedAtUnix;

		private List<Pass> _passes = new();

		public Report()
		{
		}

		public Report(Event e, List<Pass> passes)
		{
				EventID = e.ID;
				_passes = passes;
		}

		public void CalculateMetrics()
		{
				TotalRevenue = 0;
				TicketsSold = 0;
				AttendanceCount = 0;

				foreach (Pass pass in _passes)
				{
						if (pass.IsRefunded)
						{
								continue;
						}

						TotalRevenue += pass.Price;
						TicketsSold++;

						if (pass is Ticket ticket && ticket.Used)
						{
								AttendanceCount++;
						}
						else if (pass is Subscription subscription && subscription.UsedMatches.Count > 0)
						{
								AttendanceCount++;
						}
				}

				UpdatedAtUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		}

		public string ToCsv()
		{
				return "EventID,TotalRevenue,TicketsSold,AttendanceCount,UpdatedAtUnix" + Environment.NewLine +
						$"{EventID},{TotalRevenue.ToString("F2", CultureInfo.InvariantCulture)},{TicketsSold},{AttendanceCount},{UpdatedAtUnix}";
		}

		public override string ToString()
		{
				return "===== ANALYTICS REPORT =====" + Environment.NewLine +
						$"Event ID:         {EventID}" + Environment.NewLine +
						$"Total revenue:    {TotalRevenue:F2}" + Environment.NewLine +
						$"Tickets sold:     {TicketsSold}" + Environment.NewLine +
						$"Attendance:       {AttendanceCount}" + Environment.NewLine +
						$"Updated at:       {DateTimeOffset.FromUnixTimeSeconds(UpdatedAtUnix):g}" + Environment.NewLine +
						"============================";
		}
}
