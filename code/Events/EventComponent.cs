namespace FootballSystem.Events;

public interface EventComponent
{
    int getId();
    string getName();
    int getStartTimestamp();
    int getEndTimestamp();
}
