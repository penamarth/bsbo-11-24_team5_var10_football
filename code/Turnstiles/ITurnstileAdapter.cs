namespace FootballSystem.Turnstiles;

public interface ITurnstileAdapter
{
    bool unlock(int turnstileId);
    bool @lock(int turnstileId);
    string getStatus(int turnstileId);
}
