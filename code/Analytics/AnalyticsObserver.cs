namespace FootballSystem.Analytics;

public interface AnalyticsObserver
{
    void update(int eventId, int ticketsSold, double revenue);
}
