namespace FootballTicketSystem.Devices;

public sealed class Turnstile
{
    public Turnstile(int id, int stadiumId)
    {
        Id = id;
        StadiumId = stadiumId;
    }

    public int Id { get; }

    public int StadiumId { get; }

    public bool IsUnlocked { get; private set; }

    public bool PassageDetected { get; private set; }

    public void Unlock() => IsUnlocked = true;

    public void Lock()
    {
        IsUnlocked = false;
        PassageDetected = false;
    }

    public void SimulatePassage()
    {
        if (IsUnlocked)
        {
            PassageDetected = true;
        }
    }
}
