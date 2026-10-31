using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Services;

public sealed class EmailNotificationSystem : IEmailNotificationSystem
{
    private sealed record Mail(EmailAddress To, string Subject, string Body);

    private readonly List<Mail> _sent = new();
    private readonly Queue<Mail> _queue = new();

    public bool IsAvailable { get; set; } = true;

    public IReadOnlyList<string> SentMessages => _sent.Select(m => $"{m.To} | {m.Subject}").ToList();

    public int PendingCount => _queue.Count;

    public bool SendEmail(EmailAddress to, string subject, string body)
    {
        if (!IsAvailable)
        {
            return false;
        }

        _sent.Add(new Mail(to, subject, body));
        return true;
    }

    public bool SendOrQueue(EmailAddress to, string subject, string body)
    {
        var mail = new Mail(to, subject, body);
        if (IsAvailable)
        {
            _sent.Add(mail);
            return true;
        }

        _queue.Enqueue(mail);
        return false;
    }

    public int RetryQueued()
    {
        if (!IsAvailable)
        {
            return 0;
        }

        var sent = 0;
        while (_queue.Count > 0)
        {
            _sent.Add(_queue.Dequeue());
            sent++;
        }

        return sent;
    }
}
