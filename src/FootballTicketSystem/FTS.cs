using FootballTicketSystem.Bank;
using FootballTicketSystem.Devices;
using FootballTicketSystem.Domain;
using FootballTicketSystem.Presentation;
using FootballTicketSystem.Services;
using FootballTicketSystem.Storage;

namespace FootballTicketSystem;

public sealed class FTS
{
    private readonly List<Turnstile> _turnstiles = new();
    private readonly List<AnalyticsReport> _reports = new();
    private Event? _analyticsEvent;
    private IReadOnlyList<Pass> _analyticsSales = Array.Empty<Pass>();
    private AnalyticsReport? _analyticsReport;

    public FTS(TimeProvider time)
        : this(time, new Screen())
    {
    }

    public FTS(TimeProvider time, Screen screen)
    {
        Time = time;
        Screen = screen;
        Events = new EventStorage();
        Passes = new PassStorage();
        Sberbank = new SberbankAPI();
        Bank = new BankAdapter(Sberbank);
        Email = new EmailNotificationSystem();
        Server = new RemoteServerGateway();
        Audit = new AuditJournal(time);
        Security = new SecurityService();
        Cash = new CashMachine();
        Printer = new KioskPrinter(screen);
    }

    public TimeProvider Time { get; }

    public Screen Screen { get; }

    public EventStorage Events { get; }

    public PassStorage Passes { get; }

    public SberbankAPI Sberbank { get; }

    public IBankAdapter Bank { get; }

    public EmailNotificationSystem Email { get; }

    public RemoteServerGateway Server { get; }

    public AuditJournal Audit { get; }

    public SecurityService Security { get; }

    public CashMachine Cash { get; }

    public KioskPrinter Printer { get; }

    public DateTimeOffset Now => Time.GetLocalNow();

    public IReadOnlyList<AnalyticsReport> Reports => _reports;

    public IReadOnlyList<Event> GetAvailableEvents() => Events.LoadEvents().Where(e => e.OnSale).ToList();

    public StadiumMap? GetSeatMap(int eventId) => Events.FindEventById(eventId)?.Map;

    public void RequestScan() => Screen.Show("Отсканируйте QR/штрихкод билета/абонемента");

    public Turnstile AddTurnstile(int stadiumId, int turnstileId)
    {
        var turnstile = new Turnstile(turnstileId, stadiumId);
        _turnstiles.Add(turnstile);
        return turnstile;
    }

    public bool UnlockTurnstile(int stadiumId, int turnstileId)
    {
        var turnstile = _turnstiles.FirstOrDefault(t => t.StadiumId == stadiumId && t.Id == turnstileId);
        if (turnstile == null)
        {
            return false;
        }

        turnstile.Unlock();
        return true;
    }

    public bool LockTurnstile(int stadiumId, int turnstileId)
    {
        var turnstile = _turnstiles.FirstOrDefault(t => t.StadiumId == stadiumId && t.Id == turnstileId);
        if (turnstile == null)
        {
            return false;
        }

        turnstile.Lock();
        return true;
    }

    public Purchase CreatePurchase() => new();

    public Pass CreatePass(Purchase purchase)
    {
        var e = purchase.Event ?? throw new InvalidOperationException("Мероприятие не выбрано");
        var seat = purchase.Seat ?? throw new InvalidOperationException("Место не выбрано");
        var email = purchase.Email ?? throw new InvalidOperationException("Электронная почта не введена");
        var reference = purchase.Payment?.Reference ?? "CASH";

        Pass pass = e.Matches.Count == 1
            ? new Ticket(e.Matches[0].Id, seat.Id, e.PriceCoefficient, email, purchase.PaymentType, reference)
            : new Subscription(
                e.Matches.Select(m => m.Id).ToList(),
                seat.Id,
                e.PriceCoefficient,
                email,
                purchase.PaymentType,
                reference,
                e.StartTimestamp,
                e.EndTimestamp - e.StartTimestamp);

        return Passes.AddPass(pass);
    }

    public Refund? CreateRefund(int passId)
    {
        var pass = Passes.FindPassById(passId);
        if (pass == null || !Passes.RefundPass(passId))
        {
            return null;
        }

        var refund = new Refund(passId, PriceOf(pass), pass.PaymentMethod);
        return Passes.AddRefund(refund);
    }

    public Money PriceOf(Pass pass)
    {
        var seat = Events.FindEventByMatch(pass.CoveredMatches[0])?.Map?.FindSeat(pass.SeatId);
        return seat == null ? Money.Zero : pass.CalculatePrice(seat);
    }

    public AnalyticsReport? GetReport(int eventId) => _reports.LastOrDefault(r => r.EventId == eventId);

    public void SaveReport(AnalyticsReport report)
    {
        _reports.Add(report);
        Server.Synchronize($"Отчёт по мероприятию {report.EventId}");
        Audit.Record("Аналитика", $"Отчёт по мероприятию {report.EventId} сохранён");
    }

    public IReadOnlyList<Event> EventsList { get; private set; } = Array.Empty<Event>();

    public IReadOnlyList<Event> ManageAnalytics()
    {
        EventsList = Events.LoadEvents().Where(e => Passes.HasSales(e)).ToList();

        if (EventsList.Count == 0)
        {
            Screen.Show("В Event Storage нет мероприятий с данными о продажах");
            return EventsList;
        }

        Screen.Show("Выберите мероприятие для аналитики:");

        foreach (var e in EventsList)
        {
            Screen.Show($"  {e.Id}. {e.Name}");
        }

        return EventsList;
    }

    public IReadOnlyList<Pass>? SelectEvent(int eventId)
    {
        var e = Events.FindEventById(eventId);

        if (e == null)
        {
            Screen.Show("Мероприятие не найдено");
            return null;
        }

        _analyticsEvent = e;
        _analyticsSales = Passes.FindPassesByEvent(e);
        _analyticsReport = null;
        Screen.Show($"Мероприятие «{e.Name}»: продано билетов/абонементов — {_analyticsSales.Count}");
        return _analyticsSales;
    }

    public AnalyticsReport? CalculateMetrics()
    {
        if (_analyticsEvent?.Map == null)
        {
            Screen.Show("Сначала выберите мероприятие");
            return null;
        }

        _analyticsReport = new AnalyticsReport(_analyticsEvent, _analyticsEvent.Map, _analyticsSales, Now);
        _analyticsReport.CalculateMetrics();
        Screen.Show(_analyticsReport.ToString()!);
        return _analyticsReport;
    }

    public string? ExportReport(ReportFormat format)
    {
        if (_analyticsReport is not { } report)
        {
            Screen.Show("Отчёт не сформирован");
            return null;
        }

        SaveReport(report);

        var directory = Path.Combine(Directory.GetCurrentDirectory(), "reports");
        Directory.CreateDirectory(directory);
        var extension = format == ReportFormat.Csv ? "csv" : "txt";
        var content = format == ReportFormat.Csv ? report.ToCsv() : report.ToString()!;
        var path = Path.Combine(directory, $"report_{report.EventId}.{extension}");

        File.WriteAllText(path, content);
        Screen.Show($"Подтверждение: отчёт выгружен в {path}");
        return path;
    }

    public bool SendTicketEmail(EmailAddress email, Pass pass)
    {
        var subject = $"Электронная копия билета/абонемента #{pass.Id}";
        var body = $"QR-код: {pass.QrToken}";
        var sent = Email.SendOrQueue(email, subject, body);

        if (!sent)
        {
            Screen.Show("Письмо поставлено в очередь на отложенную отправку");
        }

        Audit.Record("Покупка", sent ? $"Письмо отправлено на {email}" : $"Письмо для {email} поставлено в очередь");
        return sent;
    }
}
