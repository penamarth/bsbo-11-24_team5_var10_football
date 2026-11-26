namespace FootballSystem.Analytics;

public interface AnalyticsSubject
{
    void attach(AnalyticsObserver o);
    void detach(AnalyticsObserver o);
    void notifyObservers(int eventId);
}
