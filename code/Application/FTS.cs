namespace FootballSystem.Application;

using FootballSystem.Analytics;
using FootballSystem.Banking;
using FootballSystem.Events;
using FootballSystem.Passes;
using FootballSystem.Turnstiles;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class FTS : AnalyticsSubject
{
    private PassRepository passRepo;
    private EventRepository eventRepo;
    private IBankAdapter bank;
    private ITurnstileAdapter turnstiles;
    private List<AnalyticsObserver> reports = new();
    private Event? analyticsEvent;
    private List<Pass> analyticsPasses = new();
    private Report? analyticsReport;

    private const int BookingTimeoutSeconds = 300;

    public FTS(PassRepository passRepo, EventRepository eventRepo, IBankAdapter bank, ITurnstileAdapter turnstiles)
    {
        Console.WriteLine("[ВЫЗОВ] FTS.create()");
        this.passRepo = passRepo;
        this.eventRepo = eventRepo;
        this.bank = bank;
        this.turnstiles = turnstiles;
    }

    public async Task run()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.run()");

        passRepo.createPass(new Ticket(0, 100));

        StadiumMap map = new();
        map.seats = new List<Seat>
        {
            new Seat(1, 1, 1, 1, 1000),
            new Seat(2, 2, 1, 1, 2000)
        };

        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int nextWeek = (int)DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();

        Event createdEvent = new Event(1, "Event", now, nextWeek);
        eventRepo.createEvent(createdEvent);

        if (createdEvent != null)
        {
            Match match = new Match(0, "Match", 1.5, map.clone(), now, nextWeek);
            eventRepo.createMatch(createdEvent.id, match);
            match.setSeatStatus(2, SeatStatus.Booked);
        }

        string msg = "1-5";
        for (;;)
        {
            Console.Write("\x1b[2J\x1b[H");

            Console.WriteLine("1. Use a ticket");
            Console.WriteLine("2. Buy a ticket");
            Console.WriteLine("3. Refund a ticket");
            Console.WriteLine("4. View analytics");
            Console.WriteLine("5. Manage events");

            Console.Write($"{msg}> ");
            msg = "1-5";

            string? ans = Console.ReadLine();
            switch (ans)
            {
                case "1":
                    break;
                case "2":
                    await startPurchase();
                    break;
                case "3":
                    await startRefund();
                    break;
                case "4":
                    runAnalytics();
                    break;
                case "5":
                    break;
                default:
                    msg = "Only digits between 1-5";
                    break;
            }
        }
    }

    public async Task startPurchase()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.startPurchase()");

        List<Event> events = getAvailableEvents();
        if (events.Count == 0)
        {
            Console.WriteLine("No events are open for sale right now.");
            pause();
            return;
        }

        Event? ev = selectEvent(events);
        if (ev == null) return;

        bool? isSubscription = requestPurchaseType();
        if (isSubscription == null) return;

        List<Match> seasonMatches;
        Match? match;

        if (isSubscription.Value)
        {
            int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            seasonMatches = ev.getChildren().OfType<Match>()
                .Where(m => m.getEndTimestamp() > now)
                .ToList();

            if (seasonMatches.Count == 0)
            {
                Console.WriteLine("No matches left in this season.");
                pause();
                return;
            }

            match = seasonMatches.First();
        }
        else
        {
            match = selectMatch(ev);
            if (match == null) return;
            seasonMatches = new List<Match> { match };
        }

        StadiumMap? map = getSeatMap(match.getId());
        if (map == null)
        {
            Console.WriteLine("Seat map is not available.");
            pause();
            return;
        }

        string seatMapTitle = isSubscription.Value
            ? $"subscription for {ev.getName()}"
            : match.getName();

        int? seatId;
        for (;;)
        {
            seatId = selectSeat(seatMapTitle, map, match);
            if (seatId == null) return;

            if (reserveSeats(seasonMatches, seatId.Value)) break;

            Console.WriteLine("This seat is already taken. The seat map has been updated.");
        }

        Seat? seat = map.seats.FirstOrDefault(s => s.id == seatId.Value);
        if (seat == null)
        {
            releaseSeats(seasonMatches, seatId.Value);
            return;
        }

        double price = calculatePrice(seat, seasonMatches);

        if (!showPurchaseDetails(ev, match, seat, price, isSubscription.Value, seasonMatches.Count))
        {
            releaseSeats(seasonMatches, seatId.Value);
            Console.WriteLine("Purchase cancelled.");
            return;
        }

        string? email = requestEmail();
        if (email == null)
        {
            releaseSeats(seasonMatches, seatId.Value);
            Console.WriteLine("Purchase cancelled.");
            return;
        }

        string description = isSubscription.Value
            ? $"FTS subscription: {ev.getName()}, seat {seat.id}"
            : $"FTS ticket: {ev.getName()} / {match.getName()}, seat {seat.id}";

        string? credentials = null;
        while (credentials == null)
        {
            if (!checkBooking(seasonMatches, seatId.Value))
            {
                Console.WriteLine("The seat reservation has expired, the purchase is cancelled.");
                pause();
                return;
            }

            credentials = await payByCard(description, price);

            if (credentials == null)
            {
                Console.WriteLine("Do you wish to retry the payment (y/N)?");
                if (Console.ReadLine() != "y")
                {
                    releaseSeats(seasonMatches, seatId.Value);
                    Console.WriteLine("Purchase cancelled.");
                    return;
                }
            }
        }

        Pass pass = isSubscription.Value
            ? buildSubscription(ev, seasonMatches)
            : new Ticket(match.getId(), price);

        pass.seatId = seat.id;
        pass.price = price;
        pass.email = email;
        pass.paymentMethod = PaymentType.Card;
        pass.paymentCredentials = credentials;
        passRepo.createPass(pass);
        pass.QRCode = generateQRCode(pass.id);

        foreach (Match m in seasonMatches)
        {
            ev.setSeatStatus(m.getId(), seatId.Value, SeatStatus.Sold);
        }

        showPurchaseConfirmation(pass);

        await sendTicketEmail(pass);

        pause();
    }

    public Event? selectEvent(List<Event> events)
    {
        Console.WriteLine("[ВЫЗОВ] FTS.selectEvent(events)");

        for (;;)
        {
            Console.Write("\x1b[2J\x1b[H");
            Console.WriteLine("===== EVENTS OPEN FOR SALE =====");
            for (int i = 0; i < events.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {events[i].getName()} ({formatDate(events[i].getStartTimestamp())} - {formatDate(events[i].getEndTimestamp())})");
            }
            Console.WriteLine("================================");

            Console.Write($"Select an event (1-{events.Count}), or press Enter to cancel: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= events.Count)
            {
                return events[choice - 1];
            }

            Console.WriteLine("Invalid selection.");
        }
    }

    public Match? selectMatch(Event ev)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.selectMatch(eventId={ev.getId()})");

        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        List<Match> openMatches = ev.getChildren().OfType<Match>()
            .Where(m => m.getEndTimestamp() > now)
            .ToList();

        if (openMatches.Count == 0)
        {
            Console.WriteLine("No matches of this event are open for sale.");
            pause();
            return null;
        }

        for (;;)
        {
            Console.Write("\x1b[2J\x1b[H");
            Console.WriteLine($"===== MATCHES: {ev.getName()} =====");
            for (int i = 0; i < openMatches.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {openMatches[i].getName()} ({formatDate(openMatches[i].getStartTimestamp())})");
            }
            Console.WriteLine("===============================");

            Console.Write($"Select a match (1-{openMatches.Count}), or press Enter to cancel: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= openMatches.Count)
            {
                return openMatches[choice - 1];
            }

            Console.WriteLine("Invalid selection.");
        }
    }

    public int? selectSeat(string title, StadiumMap map, Match match)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.selectSeat(title={title}, matchId={match.getId()})");

        for (;;)
        {
            Console.Write("\x1b[2J\x1b[H");
            Console.WriteLine($"===== SEAT MAP: {map.name} ({title}) =====");
            foreach (Seat s in map.seats)
            {
                string status = match.seatStatuses.GetValueOrDefault(s.id, SeatStatus.Free) switch
                {
                    SeatStatus.Free => "free",
                    SeatStatus.Booked => "booked",
                    SeatStatus.Sold => "sold",
                    _ => "blocked",
                };
                Console.WriteLine($"Seat {s.id,3} | sector {s.sector}, row {s.row}, place {s.column} | {s.price,8:F2} | {status}");
            }
            Console.WriteLine("================================");

            Console.Write("Enter a seat ID to pick (or press Enter to cancel): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (int.TryParse(input, out int seatId) && map.seats.Any(s => s.id == seatId))
            {
                return seatId;
            }

            Console.WriteLine("Invalid seat ID.");
        }
    }

    public bool? requestPurchaseType()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.requestPurchaseType()");

        Console.WriteLine("What do you wish to buy?");
        Console.WriteLine("1. A ticket for a match");
        Console.WriteLine("2. A subscription for the season");
        Console.Write("> ");

        switch (Console.ReadLine())
        {
            case "1":
                return false;
            case "2":
                return true;
            default:
                return null;
        }
    }

    public bool showPurchaseDetails(Event ev, Match match, Seat seat, double price, bool isSubscription, int matchCount)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.showPurchaseDetails(price={price:F2}, isSubscription={isSubscription})");

        Console.Write("\x1b[2J\x1b[H");
        Console.WriteLine("===== PURCHASE DETAILS =====");
        Console.WriteLine($"Event:          {ev.getName()}");
        Console.WriteLine(isSubscription
            ? $"Subscription:   all {matchCount} matches of the season"
            : $"Match:          {match.getName()} ({formatDate(match.getStartTimestamp())})");
        Console.WriteLine($"Seat:           {seat.id} (sector {seat.sector}, row {seat.row}, place {seat.column})");
        Console.WriteLine($"Total price:    {price:F2}");
        Console.WriteLine("============================");
        Console.Write("Proceed with the purchase (y/N)? ");
        return Console.ReadLine() == "y";
    }

    public string? requestEmail()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.requestEmail()");

        for (;;)
        {
            Console.Write("Enter your email for the e-pass and the receipt (or press Enter to cancel): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return null;

            string email = input.Trim();
            if (isValidEmail(email)) return email;

            Console.WriteLine("The email address is invalid, please try again.");
        }
    }

    public async Task<string?> payByCard(string description, double amount)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.payByCard(description={description}, amount={amount:F2})");

        string? url;
        try
        {
            url = await bank.startPurchase(description, amount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bank error: {ex.Message}");
            return null;
        }

        if (url == null)
        {
            Console.WriteLine("Card payment is temporarily unavailable.");
            return null;
        }

        Console.WriteLine("Complete the payment on the bank page (3-D Secure):");
        Console.WriteLine(url);
        string status = await bank.confirmPurchase(url);

        if (status != "success")
        {
            Console.WriteLine("The bank declined the payment.");
            return null;
        }

        return transactionId(url);
    }

    public async Task sendTicketEmail(Pass pass)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.sendTicketEmail(passId={pass.id})");
        string text = purchaseText(pass) + "\n" + receiptText(pass);
        await sendEmail(pass.email, text);
    }

    public void showPurchaseConfirmation(Pass pass)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.showPurchaseConfirmation(passId={pass.id})");
        Console.WriteLine("===== PURCHASE CONFIRMED =====");
        Console.WriteLine(purchaseText(pass));
        Console.WriteLine("==============================");
    }

    private Subscription buildSubscription(Event ev, List<Match> seasonMatches)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.buildSubscription(eventId={ev.getId()})");
        Subscription subscription = new(seasonMatches.Select(m => m.getId()).ToHashSet());
        subscription.startTimestamp = ev.getStartTimestamp();
        subscription.endTimestamp = ev.getEndTimestamp();
        return subscription;
    }

    private bool reserveSeats(List<Match> matches, int seatId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.reserveSeats(seatId={seatId})");
        foreach (Match match in matches)
        {
            if (!eventRepo.reserveSeat(match.getId(), seatId, BookingTimeoutSeconds))
            {
                return false;
            }
        }
        return true;
    }

    private void releaseSeats(List<Match> matches, int seatId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.releaseSeats(seatId={seatId})");
        foreach (Match match in matches)
        {
            eventRepo.releaseSeat(match.getId(), seatId);
        }
    }

    private bool checkBooking(List<Match> matches, int seatId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.checkBooking(seatId={seatId})");
        foreach (Match match in matches)
        {
            if (!eventRepo.checkBooking(match.getId(), seatId))
            {
                return false;
            }
        }
        return true;
    }

    private static double calculatePrice(Seat seat, List<Match> matches)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.calculatePrice(seatId={seat.id})");
        return matches.Sum(m => seat.price * m.priceCoefficient);
    }

    private static string transactionId(string url)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.transactionId(url={url})");
        string id = url.TrimEnd('/').Split('/').Last();
        return string.IsNullOrEmpty(id) ? url : id;
    }

    private static string generateQRCode(int passId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.generateQRCode(passId={passId})");
        return $"FTS-{passId}-{Guid.NewGuid():N}";
    }

    private static string purchaseText(Pass pass)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.purchaseText(passId={pass.id})");
        string text = "Electronic pass\n" +
            $"Pass ID:    {pass.id}\n" +
            $"Type:       {pass.GetType().Name}\n" +
            $"Seat ID:    {pass.seatId}\n" +
            $"Price:      {pass.price:F2}\n" +
            $"QR code:    {pass.QRCode}";

        if (pass is Ticket ticket) text += $"\nMatch ID:   {ticket.matchId}";
        if (pass is Subscription subscription) text += $"\nSeason:     {formatDate(subscription.startTimestamp)} - {formatDate(subscription.endTimestamp)}";

        return text;
    }

    private static string receiptText(Pass pass)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.receiptText(passId={pass.id})");
        return "E-RECEIPT\n" +
            $"Pass ID:         {pass.id}\n" +
            $"Paid:            {pass.price:F2}\n" +
            $"Payment method:  {pass.paymentMethod}";
    }

    private static bool isValidEmail(string email)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.isValidEmail(email={email})");
        int at = email.IndexOf('@');
        return at > 0
            && email.IndexOf('@', at + 1) == -1
            && email.IndexOf('.', at + 1) > at + 1
            && email[^1] != '.';
    }

    private static string formatDate(int unixTimestamp)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.formatDate(timestamp={unixTimestamp})");
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).ToString("dd.MM.yyyy HH:mm");
    }

    private static void pause()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.pause()");
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    public async Task startRefund()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.startRefund()");

        int? passId = requestScan();
        if (passId == null) return;

        bool refundAllowed = passRepo.checkRefundEligibility(passId.Value);
        while (!refundAllowed)
        {
            Console.WriteLine("Refund isn't allowed, do you wish to scan some other pass (y/N)?");
            if (Console.ReadLine() != "y") return;
            passId = requestScan();
            if (passId == null) return;
            refundAllowed = passRepo.checkRefundEligibility(passId.Value);
        }

        Pass? pass = passRepo.getPassById(passId.Value);
        if (pass == null) { Console.WriteLine("Pass not found."); return; }
        if (pass.price <= 0) { Console.WriteLine("No refundable amount on this pass."); return; }

        if (!showRefundDetails(pass)) { Console.WriteLine("Refund cancelled."); return; }

        bool refunded;
        switch (pass.paymentMethod)
        {
            case PaymentType.Card:
                refunded = await refundToCard(pass);
                if (!refunded)
                {
                    Console.WriteLine("Failed to refund to card. Do you wish to receive it in cash (y/N)?");
                    if (Console.ReadLine() != "y") return;
                    refunded = await refundWithCash(pass);
                }
                break;

            case PaymentType.Cash:
                refunded = await refundWithCash(pass);
                break;

            default:
                Console.WriteLine("Unknown payment method. Do you wish to receive the refund in cash (y/N)?");
                if (Console.ReadLine() != "y") return;
                refunded = await refundWithCash(pass);
                break;
        }

        if (!refunded) { Console.WriteLine("Refund failed."); return; }

        if (!passRepo.refund(pass.id))
            Console.WriteLine("Warning: this pass was already marked as refunded.");

        if (pass is Ticket ticket)
        {
            eventRepo.refundSeat(ticket.matchId, ticket.seatId);
        }
        else if (pass is Subscription subscription)
        {
            foreach (int id in subscription.matchIds)
            {
                eventRepo.refundSeat(id, subscription.seatId);
            }
        }

        await giveReceipt(pass);
        waitForEnter();
    }

    public bool showRefundDetails(Pass pass)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.showRefundDetails(passId={pass.id})");

        Console.Write("\x1b[2J\x1b[H");
        Console.WriteLine("===== REFUND DETAILS =====");
        Console.WriteLine($"Pass ID:          {pass.id}");
        Console.WriteLine($"Type:             {pass.GetType().Name}");
        Console.WriteLine($"Seat ID:          {pass.seatId}");
        Console.WriteLine($"Refund amount:    {pass.price:F2}");
        Console.WriteLine($"Payment method:   {pass.paymentMethod}");
        Console.WriteLine($"Already refunded: {pass.isRefunded}");
        Console.WriteLine("==========================");
        Console.Write("Proceed with the refund (y/N)? ");
        return Console.ReadLine() == "y";
    }

    public async Task<bool> refundToCard(Pass pass)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.refundToCard(passId={pass.id})");

        if (string.IsNullOrEmpty(pass.paymentCredentials))
        {
            Console.WriteLine("No card credentials on this pass.");
            return false;
        }

        Console.WriteLine($"Refunding {pass.price:F2} to card...");
        try { return await bank.refund(pass.paymentCredentials, "", pass.price); }
        catch (Exception ex) { Console.WriteLine($"Card refund error: {ex.Message}"); return false; }
    }

    public async Task<bool> refundWithCash(Pass pass)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.refundWithCash(passId={pass.id})");

        Console.WriteLine($"Please hand {pass.price:F2} to the customer.");
        await Task.Delay(500);
        Console.WriteLine("Cash refund completed.");
        return true;
    }

    public int? requestScan()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.requestScan()");

        Console.Write("\x1b[2J\x1b[H");
        Console.Write("Scan your pass (enter pass ID, or press Enter to cancel): ");
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (!int.TryParse(input, out int passId))
        {
            Console.WriteLine("Invalid pass ID.");
            return null;
        }

        return passId;
    }

    public async Task giveReceipt(Pass pass)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.giveReceipt(passId={pass.id})");

        string text = "Ticket for a refund\n" +
            $"Refunded: {pass.price:F2}";
        await Task.WhenAll(sendEmail(pass.email, text), printReceipt(text));
    }

    private async Task<bool> printReceipt(string text)
    {
        Console.WriteLine("[ВЫЗОВ] FTS.printReceipt()");

        Console.WriteLine("Printing a receipt...");
        await Task.Delay(2000);
        Console.WriteLine("===== RECEIPT =====");
        Console.WriteLine(text);
        Console.WriteLine("===================");
        return true;
    }

    private async Task<bool> sendEmail(string email, string text)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.sendEmail(email={email})");

        Console.WriteLine($"Sending an email to {email}...");
        await Task.Delay(1000);
        Console.WriteLine("====== EMAIL ======");
        Console.WriteLine(text);
        Console.WriteLine("===================");
        return true;
    }

    public void runAnalytics()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.runAnalytics()");

        List<Event> events = manageAnalytics();
        if (events.Count == 0)
        {
            waitForEnter();
            return;
        }

        Console.Write("Select an event ID (or press Enter to cancel): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (!int.TryParse(input, out int eventId))
        {
            Console.WriteLine("Invalid event ID.");
            waitForEnter();
            return;
        }

        if (!selectEvent(eventId))
        {
            waitForEnter();
            return;
        }

        Report? report = calculateMetrics();
        if (report == null)
        {
            waitForEnter();
            return;
        }

        Console.Write("Export the report (c - CSV, t - TXT, Enter - skip): ");
        string? choice = Console.ReadLine();
        switch (choice)
        {
            case "c":
                exportReport(ReportFormat.Csv);
                break;
            case "t":
                exportReport(ReportFormat.Txt);
                break;
        }

        waitForEnter();
    }

    public List<Event> manageAnalytics()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.manageAnalytics()");

        List<Event> events = eventRepo.getEvents()
            .Where(e =>
            {
                HashSet<int> matchIds = e.getChildren().OfType<Match>().Select(m => m.getId()).ToHashSet();
                return passRepo.getPassesWithFilter(p => passRepo.coversAnyMatch(p, matchIds)).Count > 0;
            })
            .ToList();

        Console.Write("\x1b[2J\x1b[H");
        Console.WriteLine("===== ANALYTICS: EVENTS =====");

        if (events.Count == 0)
        {
            Console.WriteLine("No events with sales data.");
        }
        else
        {
            foreach (Event e in events)
            {
                Console.WriteLine($"{e.getId()}. {e.getName()}");
            }
        }

        Console.WriteLine("============================");
        return events;
    }

    public bool selectEvent(int eventId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.selectEvent(eventId={eventId})");

        Event? e = eventRepo.findEventById(eventId);
        if (e == null)
        {
            Console.WriteLine("Event not found.");
            return false;
        }

        analyticsEvent = e;
        analyticsReport = null;
        HashSet<int> matchIds = e.getChildren().OfType<Match>().Select(m => m.getId()).ToHashSet();
        analyticsPasses = passRepo.getPassesWithFilter(p => passRepo.coversAnyMatch(p, matchIds));

        Console.WriteLine($"Event: {e.getName()}");
        Console.WriteLine($"Tickets/subscriptions found: {analyticsPasses.Count}");
        return true;
    }

    public Report? calculateMetrics()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.calculateMetrics()");

        if (analyticsEvent == null)
        {
            Console.WriteLine("Select an event first.");
            return null;
        }

        int eventId = analyticsEvent.getId();

        foreach (AnalyticsObserver observer in reports.ToList())
        {
            if (observer is Report oldReport && oldReport.eventId == eventId)
            {
                detach(observer);
            }
        }

        Report report = new(analyticsEvent, analyticsPasses);
        attach(report);
        notifyObservers(eventId);
        report.calculateMetrics();

        analyticsReport = report;

        Console.WriteLine(report);
        return report;
    }

    public string? exportReport(ReportFormat format)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.exportReport(format={format})");

        if (analyticsReport == null)
        {
            Console.WriteLine("No report has been calculated yet.");
            return null;
        }

        string directory = Path.Combine(Directory.GetCurrentDirectory(), "reports");
        Directory.CreateDirectory(directory);

        string extension = format == ReportFormat.Csv ? "csv" : "txt";
        string path = Path.Combine(directory, $"report_{analyticsReport.eventId}.{extension}");
        string content = format == ReportFormat.Csv ? analyticsReport.toCsv() : analyticsReport.ToString();

        File.WriteAllText(path, content);
        Console.WriteLine($"Report exported to {path}");
        return path;
    }

    private static void waitForEnter()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.waitForEnter()");
        Console.Write("Press Enter to return to the menu...");
        Console.ReadLine();
    }

    public Report? getReport(int eventId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.getReport(eventId={eventId})");
        return reports.OfType<Report>().FirstOrDefault(r => r.eventId == eventId);
    }

    public List<Event> getAvailableEvents()
    {
        Console.WriteLine("[ВЫЗОВ] FTS.getAvailableEvents()");
        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return eventRepo.getEvents().Where(e => e.getEndTimestamp() > now).ToList();
    }

    public StadiumMap? getSeatMap(int matchId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.getSeatMap(matchId={matchId})");
        Match? match = eventRepo.findMatchById(matchId);
        return match?.stadiumMap.clone();
    }

    public bool unlockTurnstile(int turnstileId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.unlockTurnstile(turnstileId={turnstileId})");
        return turnstiles.unlock(turnstileId);
    }

    public bool lockTurnstile(int turnstileId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.lockTurnstile(turnstileId={turnstileId})");
        return turnstiles.@lock(turnstileId);
    }

    public void attach(AnalyticsObserver o)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.attach(o={o.GetType().Name})");
        reports.Add(o);
    }

    public void detach(AnalyticsObserver o)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.detach(o={o.GetType().Name})");
        reports.Remove(o);
    }

    public void notifyObservers(int eventId)
    {
        Console.WriteLine($"[ВЫЗОВ] FTS.notifyObservers(eventId={eventId})");
        if (analyticsEvent == null || analyticsEvent.getId() != eventId)
        {
            return;
        }

        int ticketsSold = analyticsPasses.Count(p => !p.isRefunded);
        double revenue = analyticsPasses.Where(p => !p.isRefunded).Sum(p => p.price);

        foreach (AnalyticsObserver observer in reports)
        {
            observer.update(eventId, ticketsSold, revenue);
        }
    }
}
