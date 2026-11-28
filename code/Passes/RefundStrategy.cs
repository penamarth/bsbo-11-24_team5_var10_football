namespace FootballSystem.Passes;

using FootballSystem.Events;

public interface RefundStrategy
{
    void releaseSeats(Pass pass, EventRepository eventRepo);
}
