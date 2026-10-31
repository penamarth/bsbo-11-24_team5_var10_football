using FootballTicketSystem;
using FootballTicketSystem.Controllers;
using FootballTicketSystem.Devices;
using FootballTicketSystem.Domain;

static void SeedEvents(FTS fts)
{
    var now = fts.Now;

    var cupMap = StadiumMap.CreateStandard("Арена");
    var cup = new Event(0, "Кубок: Зенит — Спартак", now.AddHours(1), now.AddHours(3));
    cup.CreateMatch(new Match(0, 0, "Зенит — Спартак", cupMap.Name, now.AddHours(2), now.AddHours(4))
    {
        Map = cupMap
    });
    fts.Events.CreateEvent(cup);

    var seasonMap = StadiumMap.CreateStandard("Арена");
    var season = new Event(0, "Сезон 2026", now, now.AddDays(30));
    season.CreateMatch(new Match(0, 0, "Зенит — ЦСКА", seasonMap.Name, now.AddDays(7), now.AddDays(7).AddHours(2))
    {
        Map = seasonMap
    });
    season.CreateMatch(new Match(0, 0, "Зенит — Краснодар", seasonMap.Name, now.AddDays(14), now.AddDays(14).AddHours(2))
    {
        Map = seasonMap
    });
    fts.Events.CreateEvent(season);
}

var fts = new FTS(TimeProvider.System);
SeedEvents(fts);

fts.Screen.Show("=== Покупка билета на матч (карта) ===");
var ticketPurchase = new PurchaseController(fts);
ticketPurchase.StartPurchase();
ticketPurchase.SelectEvent(1);
ticketPurchase.SelectSeat(3);
ticketPurchase.ConfirmPurchase(PaymentType.Card);
ticketPurchase.EnterEmail("fan@club.ru");
var ticket = ticketPurchase.MakePayment(new Money(1500));

fts.Screen.Show(string.Empty);
fts.Screen.Show("=== Покупка абонемента (наличные) ===");
var seasonPurchase = new PurchaseController(fts);
seasonPurchase.StartPurchase();
seasonPurchase.SelectEvent(2);
seasonPurchase.SelectSeat(7);
seasonPurchase.ConfirmPurchase(PaymentType.Cash);
seasonPurchase.EnterEmail("supporter@club.ru");
var subscription = seasonPurchase.MakePayment(new Money(5000));

fts.Screen.Show(string.Empty);
fts.Screen.Show("=== Проход на стадион по QR-коду ===");
var scanner = new QrScanner();
var turnstile = fts.AddTurnstile(1, 7);
var entry = new EntryController(fts, turnstile, 1);
entry.RequestScan();
entry.ScanTicket(scanner.Scan(ticket!.QrToken)!);
turnstile.SimulatePassage();
entry.PassThrough();
entry.RequestScan();
entry.ScanTicket(scanner.Scan(ticket.QrToken)!);

fts.Screen.Show(string.Empty);
fts.Screen.Show("=== Возврат билета ===");
var refundPurchase = new PurchaseController(fts);
refundPurchase.StartPurchase();
refundPurchase.SelectEvent(1);
refundPurchase.SelectSeat(12);
refundPurchase.ConfirmPurchase(PaymentType.Cash);
refundPurchase.EnterEmail("fan2@club.ru");
var refundableTicket = refundPurchase.MakePayment(new Money(2000));

var refund = new RefundController(fts);
refund.StartRefund();
refund.ScanTicket(scanner.Scan(refundableTicket!.QrToken)!);
refund.ConfirmRefund();

fts.Screen.Show(string.Empty);
fts.Screen.Show("=== Вывод аналитики по мероприятию ===");
fts.ManageAnalytics();
fts.SelectEvent(1);
fts.CalculateMetrics();
fts.ExportReport(ReportFormat.Csv);
fts.Screen.Show($"Сохранённый отчёт: {fts.GetReport(1)}");

fts.Screen.Show(string.Empty);
fts.Screen.Show("=== Создание и редактирование мероприятия ===");
var manageEvents = new EventController(fts);
manageEvents.ManageEvent();
manageEvents.CreateNewEvent("Осенний кубок");
manageEvents.ConfigureSeat(1, SeatStatus.Blocked);
manageEvents.SaveEvent();
manageEvents.SelectEvent("Кубок: Зенит — Спартак");
manageEvents.ConfigureSeat(5, SeatStatus.Blocked);
manageEvents.SaveEvent();

fts.Screen.Show(string.Empty);
fts.Screen.Show("=== Журнал аудита ===");
foreach (var record in fts.Audit.Records)
{
    fts.Screen.Show(record);
}

