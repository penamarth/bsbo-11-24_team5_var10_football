namespace FootballSystem.Analytics;

using FootballSystem.Events;
using FootballSystem.Passes;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class Report : AnalyticsObserver
{
    public int eventId;
    public double totalRevenue;
    public int ticketsSold;
    public int attendanceCount;
    public int updatedAt;

    private List<Pass> passes = new();

    public Report()
    {
        Console.WriteLine("[ВЫЗОВ] Report.create()");
    }

    public Report(Event e, List<Pass> passes)
    {
        Console.WriteLine($"[ВЫЗОВ] Report.create(eventId={e.getId()})");
        this.eventId = e.getId();
        this.passes = passes;
    }

    public void update(int eventId, int ticketsSold, double revenue)
    {
        Console.WriteLine($"[ВЫЗОВ] Report.update(eventId={eventId}, ticketsSold={ticketsSold}, revenue={revenue:F2})");
        this.eventId = eventId;
        this.ticketsSold = ticketsSold;
        totalRevenue = revenue;
        updatedAt = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public void calculateMetrics()
    {
        Console.WriteLine("[ВЫЗОВ] Report.calculateMetrics()");
        attendanceCount = passes.Count(p =>
            !p.isRefunded &&
            ((p is Ticket ticket && ticket.used) ||
             (p is Subscription subscription && subscription.usedMatches.Count > 0)));
    }

    public string toCsv()
    {
        Console.WriteLine("[ВЫЗОВ] Report.toCsv()");
        return "EventID,TotalRevenue,TicketsSold,AttendanceCount,UpdatedAtUnix" + Environment.NewLine +
            $"{eventId},{totalRevenue.ToString("F2", CultureInfo.InvariantCulture)},{ticketsSold},{attendanceCount},{updatedAt}";
    }

    public override string ToString()
    {
        Console.WriteLine("[ВЫЗОВ] Report.ToString()");
        return "===== ANALYTICS REPORT =====" + Environment.NewLine +
            $"Event ID:         {eventId}" + Environment.NewLine +
            $"Total revenue:    {totalRevenue:F2}" + Environment.NewLine +
            $"Tickets sold:     {ticketsSold}" + Environment.NewLine +
            $"Attendance:       {attendanceCount}" + Environment.NewLine +
            $"Updated at:       {DateTimeOffset.FromUnixTimeSeconds(updatedAt):g}" + Environment.NewLine +
            "============================";
    }
}
