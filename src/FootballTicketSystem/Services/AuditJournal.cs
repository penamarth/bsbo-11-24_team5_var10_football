namespace FootballTicketSystem.Services;

public sealed class AuditJournal
{
    private readonly TimeProvider _time;
    private readonly List<string> _records = new();

    public AuditJournal(TimeProvider time) => _time = time;

    public IReadOnlyList<string> Records => _records;

    public void Record(string operation, string result) =>
        _records.Add($"{_time.GetLocalNow():yyyy-MM-dd HH:mm:ss} | {operation} | {result}");
}
