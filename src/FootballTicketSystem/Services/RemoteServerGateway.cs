namespace FootballTicketSystem.Services;

public sealed class RemoteServerGateway
{
    private readonly List<string> _sent = new();
    private readonly Queue<string> _queue = new();

    public bool IsAvailable { get; set; } = true;

    public IReadOnlyList<string> Sent => _sent;

    public int PendingCount => _queue.Count;

    public void Synchronize(string payload)
    {
        if (IsAvailable)
        {
            _sent.Add(payload);
        }
        else
        {
            _queue.Enqueue(payload);
        }
    }

    public int FlushPending()
    {
        if (!IsAvailable)
        {
            return 0;
        }

        var flushed = 0;
        while (_queue.Count > 0)
        {
            _sent.Add(_queue.Dequeue());
            flushed++;
        }

        return flushed;
    }
}
