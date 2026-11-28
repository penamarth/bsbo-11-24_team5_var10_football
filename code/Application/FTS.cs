namespace FootballSystem.Application;

using FootballSystem.UI;


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
        ConsolePanes.log("FTS.create()");
        this.passRepo = passRepo;
        this.eventRepo = eventRepo;
        this.bank = bank;
        this.turnstiles = turnstiles;
        passRepo.PassChanged += refreshReports;
    }


    public async Task run()
    {
        ConsolePanes.log("FTS.run()");

        string msg = "1-5";
        for (; ; )
        {
            ConsolePanes.clearMain();

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
                    await startUseTicket();
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
                    manageEvents();
                    break;
                default:
                    msg = "Only digits between 1-5";
                    break;
            }
        }
    }

    public async Task startUseTicket()
    {
        ConsolePanes.log("FTS.startUseTicket()");

        Match? match = selectEntryMatch();
        if (match == null) return;

        int? passId = requestScan();
        if (passId == null) return;

        Pass? pass = passRepo.getPassById(passId.Value);
        if (pass == null)
        {
            Console.WriteLine("Pass not found.");
            waitForEnter();
            return;
        }

        if (pass.isRefunded || !pass.checkAccess(match.getId()))
        {
            Console.WriteLine("Access denied: the pass is refunded, already used, or not valid for this match.");
            waitForEnter();
            return;
        }

        Seat? seat = match.stadiumMap.seats.FirstOrDefault(s => s.id == pass.seatId);
        if (seat == null)
        {
            Console.WriteLine("Seat not found on the stadium map.");
            waitForEnter();
            return;
        }

        Console.WriteLine($"Pass ID:  {pass.id}");
        Console.WriteLine($"Match:    {match.getName()} ({formatDate(match.getStartTimestamp())})");
        Console.WriteLine($"Seat:     {seat.id} (sector {seat.sector}, row {seat.row}, place {seat.column})");

        int turnstileId = readInt("Turnstile ID", 1);
        if (!unlockTurnstile(turnstileId))
        {
            Console.WriteLine("Failed to unlock the turnstile.");
            waitForEnter();
            return;
        }

        Console.Write("Press Enter after passing through...");
        Console.ReadLine();

        if (!pass.markUsed(match.getId()))
        {
            lockTurnstile(turnstileId);
            Console.WriteLine("Failed to mark the pass as used.");
            waitForEnter();
            return;
        }

        lockTurnstile(turnstileId);
        Console.WriteLine("Access granted.");
        waitForEnter();
    }

    private Match? selectEntryMatch()
    {
        ConsolePanes.log("FTS.selectEntryMatch()");

        List<Match> matches = eventRepo.getEvents().SelectMany(e => e.getChildren().OfType<Match>()).ToList();
        ConsolePanes.clearMain();

        if (matches.Count == 0)
        {
            Console.WriteLine("No matches are available.");
            waitForEnter();
            return null;
        }

        Console.WriteLine("===== MATCHES =====");
        foreach (Match m in matches)
        {
            Console.WriteLine($"{m.getId()}. {m.getName()} ({formatDate(m.getStartTimestamp())})");
        }
        Console.WriteLine("===================");

        Console.Write("Select a match ID (or press Enter to cancel): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return null;

        if (!int.TryParse(input, out int matchId))
        {
            Console.WriteLine("Invalid match ID.");
            waitForEnter();
            return null;
        }

        Match? match = eventRepo.findMatchById(matchId);
        if (match == null)
        {
            Console.WriteLine("Match not found.");
            waitForEnter();
        }

        return match;
    }

    public async Task startPurchase()
    {
        ConsolePanes.log("FTS.startPurchase()");

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

        PurchaseStrategy purchaseStrategy = isSubscription.Value
            ? new SubscriptionPurchaseStrategy()
            : new TicketPurchaseStrategy();

        Match? match = null;
        if (purchaseStrategy.needsMatchSelection())
        {
            match = selectMatch(ev);
            if (match == null) return;
        }

        List<Match> seasonMatches = purchaseStrategy.getMatches(ev, match);
        if (seasonMatches.Count == 0)
        {
            Console.WriteLine("No matches left in this season.");
            pause();
            return;
        }

        match ??= seasonMatches.First();

        StadiumMap? map = getSeatMap(match.getId());
        if (map == null)
        {
            Console.WriteLine("Seat map is not available.");
            pause();
            return;
        }

        string seatMapTitle = purchaseStrategy.getTitle(ev, match);

        int? seatId;
        for (; ; )
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

        double price = purchaseStrategy.calculatePrice(seat, seasonMatches);

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

        string description = purchaseStrategy.getDescription(ev, match, seat);

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

        Pass pass = purchaseStrategy.createPass(ev, seasonMatches, seat, price);

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
        ConsolePanes.log("FTS.selectEvent(events)");

        for (; ; )
        {
            ConsolePanes.clearMain();
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
        ConsolePanes.log($"FTS.selectMatch(eventId={ev.getId()})");

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

        for (; ; )
        {
            ConsolePanes.clearMain();
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
        ConsolePanes.log($"FTS.selectSeat(title={title}, matchId={match.getId()})");

        for (; ; )
        {
            ConsolePanes.clearMain();
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
        ConsolePanes.log("FTS.requestPurchaseType()");

        ConsolePanes.clearMain();
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
        ConsolePanes.log($"FTS.showPurchaseDetails(price={price:F2}, isSubscription={isSubscription})");

        ConsolePanes.clearMain();
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
        ConsolePanes.log("FTS.requestEmail()");

        for (; ; )
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
        ConsolePanes.log($"FTS.payByCard(description={description}, amount={amount:F2})");

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

        Console.WriteLine("Complete the payment on the bank page:");
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
        ConsolePanes.log($"FTS.sendTicketEmail(passId={pass.id})");
        string text = purchaseText(pass) + "\n" + receiptText(pass);
        await sendEmail(pass.email, text);
    }

    public void showPurchaseConfirmation(Pass pass)
    {
        ConsolePanes.log($"FTS.showPurchaseConfirmation(passId={pass.id})");
        Console.WriteLine("===== PURCHASE CONFIRMED =====");
        Console.WriteLine(purchaseText(pass));
        Console.WriteLine("==============================");
    }

    private bool reserveSeats(List<Match> matches, int seatId)
    {
        ConsolePanes.log($"FTS.reserveSeats(seatId={seatId})");
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
        ConsolePanes.log($"FTS.releaseSeats(seatId={seatId})");
        foreach (Match match in matches)
        {
            eventRepo.releaseSeat(match.getId(), seatId);
        }
    }

    private bool checkBooking(List<Match> matches, int seatId)
    {
        ConsolePanes.log($"FTS.checkBooking(seatId={seatId})");
        foreach (Match match in matches)
        {
            if (!eventRepo.checkBooking(match.getId(), seatId))
            {
                return false;
            }
        }
        return true;
    }

    private static string transactionId(string url)
    {
        ConsolePanes.log($"FTS.transactionId(url={url})");
        string id = url.TrimEnd('/').Split('/').Last();
        return string.IsNullOrEmpty(id) ? url : id;
    }

    private static string generateQRCode(int passId)
    {
        ConsolePanes.log($"FTS.generateQRCode(passId={passId})");
        return $"FTS-{passId}-{Guid.NewGuid():N}";
    }

    private static string purchaseText(Pass pass)
    {
        ConsolePanes.log($"FTS.purchaseText(passId={pass.id})");
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
        ConsolePanes.log($"FTS.receiptText(passId={pass.id})");
        return "E-RECEIPT\n" +
            $"Pass ID:         {pass.id}\n" +
            $"Paid:            {pass.price:F2}\n" +
            $"Payment method:  {pass.paymentMethod}";
    }

    private static bool isValidEmail(string email)
    {
        ConsolePanes.log($"FTS.isValidEmail(email={email})");
        int at = email.IndexOf('@');
        return at > 0
            && email.IndexOf('@', at + 1) == -1
            && email.IndexOf('.', at + 1) > at + 1
            && email[^1] != '.';
    }

    private static string formatDate(int unixTimestamp)
    {
        ConsolePanes.log($"FTS.formatDate(timestamp={unixTimestamp})");
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).ToString("dd.MM.yyyy HH:mm");
    }

    private static void pause()
    {
        ConsolePanes.log("FTS.pause()");
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    public async Task startRefund()
    {
        ConsolePanes.log("FTS.startRefund()");

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

        RefundStrategy refundStrategy = pass is Subscription
            ? new SubscriptionRefundStrategy()
            : new TicketRefundStrategy();
        refundStrategy.releaseSeats(pass, eventRepo);

        await giveReceipt(pass);
        waitForEnter();
    }

    public bool showRefundDetails(Pass pass)
    {
        ConsolePanes.log($"FTS.showRefundDetails(passId={pass.id})");

        ConsolePanes.clearMain();
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
        ConsolePanes.log($"FTS.refundToCard(passId={pass.id})");

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
        ConsolePanes.log($"FTS.refundWithCash(passId={pass.id})");

        Console.WriteLine($"Please hand {pass.price:F2} to the customer.");
        await Task.Delay(500);
        Console.WriteLine("Cash refund completed.");
        return true;
    }

    public int? requestScan()
    {
        ConsolePanes.log("FTS.requestScan()");

        ConsolePanes.clearMain();
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
        ConsolePanes.log($"FTS.giveReceipt(passId={pass.id})");

        string text = "Ticket for a refund\n" + $"Refunded: {pass.price:F2}";
        await Task.WhenAll(sendEmail(pass.email, text), printReceipt(text));
    }

    private async Task<bool> printReceipt(string text)
    {
        ConsolePanes.log("FTS.printReceipt()");

        Console.WriteLine("Printing a receipt...");
        await Task.Delay(2000);
        Console.WriteLine("===== RECEIPT =====");
        Console.WriteLine(text);
        Console.WriteLine("===================");
        return true;
    }

    private async Task<bool> sendEmail(string email, string text)
    {
        ConsolePanes.log($"FTS.sendEmail(email={email})");

        Console.WriteLine($"Sending an email to {email}...");
        await Task.Delay(1000);
        Console.WriteLine("====== EMAIL ======");
        Console.WriteLine(text);
        Console.WriteLine("===================");
        return true;
    }

    public void runAnalytics()
    {
        ConsolePanes.log("FTS.runAnalytics()");

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
        ConsolePanes.log("FTS.manageAnalytics()");

        List<Event> events = eventRepo.getEvents()
            .Where(e =>
            {
                HashSet<int> matchIds = e.getChildren().OfType<Match>().Select(m => m.getId()).ToHashSet();
                return passRepo.getPassesWithFilter(p => passRepo.coversAnyMatch(p, matchIds)).Count > 0;
            })
            .ToList();

        ConsolePanes.clearMain();
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
        ConsolePanes.log($"FTS.selectEvent(eventId={eventId})");

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
        ConsolePanes.log("FTS.calculateMetrics()");

        if (analyticsEvent == null)
        {
            Console.WriteLine("Select an event first.");
            return null;
        }

        Report report = buildReport(analyticsEvent, analyticsPasses);
        analyticsReport = report;

        Console.WriteLine(report);
        return report;
    }

    private Report buildReport(Event e, List<Pass> passes)
    {
        ConsolePanes.log($"FTS.buildReport(eventId={e.getId()})");

        foreach (AnalyticsObserver observer in reports.ToList())
        {
            if (observer is Report oldReport && oldReport.eventId == e.getId())
            {
                detach(observer);
            }
        }

        Report report = new(e, passes);
        attach(report);
        notifyObservers(e.getId());
        report.calculateMetrics();
        return report;
    }

    private void refreshReports(Pass changedPass)
    {
        ConsolePanes.log($"FTS.refreshReports(passId={changedPass.id})");

        foreach (Report existingReport in reports.OfType<Report>().ToList())
        {
            Event? e = eventRepo.findEventById(existingReport.eventId);
            if (e == null)
            {
                continue;
            }

            HashSet<int> matchIds = e.getChildren().OfType<Match>().Select(m => m.getId()).ToHashSet();
            if (!passRepo.coversAnyMatch(changedPass, matchIds))
            {
                continue;
            }

            List<Pass> passes = passRepo.getPassesWithFilter(p => passRepo.coversAnyMatch(p, matchIds));
            Report report = buildReport(e, passes);
            if (analyticsReport?.eventId == e.getId())
            {
                analyticsReport = report;
            }
        }
    }

    public string? exportReport(ReportFormat format)
    {
        ConsolePanes.log($"FTS.exportReport(format={format})");

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
        ConsolePanes.log("FTS.waitForEnter()");
        Console.Write("Press Enter to return to the menu...");
        Console.ReadLine();
    }

    public Report? getReport(int eventId)
    {
        ConsolePanes.log($"FTS.getReport(eventId={eventId})");
        return reports.OfType<Report>().FirstOrDefault(r => r.eventId == eventId);
    }

    public List<Event> getAvailableEvents()
    {
        ConsolePanes.log("FTS.getAvailableEvents()");
        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return eventRepo.getEvents().Where(e => e.getEndTimestamp() > now).ToList();
    }

    public StadiumMap? getSeatMap(int matchId)
    {
        ConsolePanes.log($"FTS.getSeatMap(matchId={matchId})");
        Match? match = eventRepo.findMatchById(matchId);
        return match?.stadiumMap.clone();
    }

    public bool unlockTurnstile(int turnstileId)
    {
        ConsolePanes.log($"FTS.unlockTurnstile(turnstileId={turnstileId})");
        return turnstiles.unlock(turnstileId);
    }

    public bool lockTurnstile(int turnstileId)
    {
        ConsolePanes.log($"FTS.lockTurnstile(turnstileId={turnstileId})");
        return turnstiles.@lock(turnstileId);
    }

    public void manageEvents()
    {
        ConsolePanes.log("FTS.manageEvents()");

        string msg = "";
        for (; ; )
        {
            ConsolePanes.clearMain();
            if (msg.Length > 0)
            {
                Console.WriteLine(msg);
                msg = "";
            }

            Console.WriteLine("===== EVENT MANAGEMENT =====");
            Console.WriteLine("1. Show events and matches");
            Console.WriteLine("2. Create an event");
            Console.WriteLine("3. Stadium maps");
            Console.WriteLine("4. Create a match");
            Console.WriteLine("5. Update an event");
            Console.WriteLine("6. Update a match");
            Console.WriteLine("7. Change a seat status");
            Console.WriteLine("0. Back");
            Console.Write("> ");

            switch (Console.ReadLine())
            {
                case "1":
                    showEvents();
                    break;
                case "2":
                    createEvent();
                    break;
                case "3":
                    manageStadiumMaps();
                    break;
                case "4":
                    createMatch();
                    break;
                case "5":
                    updateEvent();
                    break;
                case "6":
                    updateMatch();
                    break;
                case "7":
                    updateSeat();
                    break;
                case "0":
                    return;
                default:
                    msg = "Only digits between 0-7";
                    break;
            }
        }
    }

    public void showEvents()
    {
        ConsolePanes.log("FTS.showEvents()");
        ConsolePanes.clearMain();

        List<Event> events = eventRepo.getEvents();
        if (events.Count == 0)
        {
            Console.WriteLine("No events are available.");
        }

        foreach (Event e in events)
        {
            Console.WriteLine($"Event {e.getId()}: {e.getName()} ({formatDate(e.getStartTimestamp())} - {formatDate(e.getEndTimestamp())})");
            foreach (EventComponent child in e.getChildren())
            {
                if (child is Match match)
                {
                    Console.WriteLine($"  Match {match.getId()}: {match.getName()} ({formatDate(match.getStartTimestamp())})");
                    Console.WriteLine($"    Stadium map {match.stadiumMap.id}: {match.stadiumMap.name} ({match.stadiumMap.seats.Count} seats)");
                }
            }
        }

        waitForEnter();
    }

    public Event? createEvent()
    {
        ConsolePanes.log("FTS.createEvent()");
        ConsolePanes.clearMain();

        Console.Write("Event name: ");
        string? name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int start = readTimestamp("Start (yyyy-MM-dd HH:mm)", now);
        int end = readTimestamp("End (yyyy-MM-dd HH:mm)", start + 7 * 24 * 3600);
        if (end <= start)
        {
            Console.WriteLine("The end must be after the start.");
            waitForEnter();
            return null;
        }

        Event created = new(0, name.Trim(), start, end);
        eventRepo.createEvent(created);
        Console.WriteLine($"Event created with ID {created.getId()}.");
        waitForEnter();
        return created;
    }

    public void manageStadiumMaps()
    {
        ConsolePanes.log("FTS.manageStadiumMaps()");

        string msg = "";
        for (; ; )
        {
            ConsolePanes.clearMain();
            if (msg.Length > 0)
            {
                Console.WriteLine(msg);
                msg = "";
            }

            Console.WriteLine("===== STADIUM MAP MANAGEMENT =====");
            Console.WriteLine("1. Show stadium maps");
            Console.WriteLine("2. Create a stadium map");
            Console.WriteLine("0. Back");
            Console.Write("> ");

            switch (Console.ReadLine())
            {
                case "1":
                    showStadiumMaps();
                    break;
                case "2":
                    createStadiumMap();
                    break;
                case "0":
                    return;
                default:
                    msg = "Only digits between 0-2";
                    break;
            }
        }
    }

    public void showStadiumMaps()
    {
        ConsolePanes.log("FTS.showStadiumMaps()");
        ConsolePanes.clearMain();

        List<StadiumMap> maps = eventRepo.getStadiumMaps();
        if (maps.Count == 0)
        {
            Console.WriteLine("No stadium maps are available.");
        }
        else
        {
            foreach (StadiumMap map in maps)
            {
                Console.WriteLine($"Map {map.id}: {map.name} ({map.seats.Count} seats, {map.turnstiles.Count} turnstiles)");
            }
        }

        waitForEnter();
    }

    public StadiumMap? createStadiumMap()
    {
        ConsolePanes.log("FTS.createStadiumMap()");
        ConsolePanes.clearMain();

        Console.Write("Stadium map name: ");
        string? name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        int seatCount = Math.Max(1, readInt("Number of seats", 2));
        double basePrice = readDouble("Base seat price", 1000);

        StadiumMap map = new();
        map.name = name.Trim();
        for (int i = 1; i <= seatCount; i++)
        {
            map.seats.Add(new Seat(i, i / 4 + 1, i % 4 + 1, i, basePrice));
        }
        map.turnstiles.Add(1);

        eventRepo.createStadiumMap(map);
        Console.WriteLine($"Stadium map created with ID {map.id}.");
        return map;
    }

    public StadiumMap? selectStadiumMap()
    {
        ConsolePanes.log("FTS.selectStadiumMap()");

        for (; ; )
        {
            List<StadiumMap> maps = eventRepo.getStadiumMaps();
            ConsolePanes.clearMain();
            Console.WriteLine("===== SELECT STADIUM MAP =====");

            foreach (StadiumMap availableMap in maps)
            {
                Console.WriteLine($"{availableMap.id}. {availableMap.name} ({availableMap.seats.Count} seats)");
            }

            Console.WriteLine("0. Create a new stadium map");
            Console.Write("Select a stadium map ID (or press Enter to cancel): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (!int.TryParse(input, out int mapId))
            {
                Console.WriteLine("Invalid stadium map ID.");
                waitForEnter();
                continue;
            }

            if (mapId == 0)
            {
                StadiumMap? created = createStadiumMap();
                if (created != null)
                {
                    return created;
                }
                continue;
            }

            StadiumMap? map = eventRepo.findStadiumMapById(mapId);
            if (map == null)
            {
                Console.WriteLine("Stadium map not found.");
                waitForEnter();
                continue;
            }

            return map;
        }
    }

    public Match? createMatch()
    {
        ConsolePanes.log("FTS.createMatch()");
        ConsolePanes.clearMain();

        List<Event> events = eventRepo.getEvents();
        if (events.Count == 0)
        {
            Console.WriteLine("Create an event first.");
            waitForEnter();
            return null;
        }

        foreach (Event e in events)
        {
            Console.WriteLine($"{e.getId()}. {e.getName()}");
        }

        Event? ev = eventRepo.findEventById(readInt("Event ID", events[0].getId()));
        if (ev == null)
        {
            Console.WriteLine("Event not found.");
            waitForEnter();
            return null;
        }

        Console.Write("Match name: ");
        string? name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        double coefficient = readDouble("Price coefficient", 1.0);
        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int start = readTimestamp("Start (yyyy-MM-dd HH:mm)", now);
        int end = readTimestamp("End (yyyy-MM-dd HH:mm)", start + 3 * 3600);
        if (end <= start)
        {
            Console.WriteLine("The end must be after the start.");
            waitForEnter();
            return null;
        }

        StadiumMap? selectedMap = selectStadiumMap();
        if (selectedMap == null)
        {
            Console.WriteLine("Match creation cancelled.");
            waitForEnter();
            return null;
        }

        StadiumMap map = selectedMap.clone();
        Match created = new(0, name.Trim(), coefficient, map, start, end);
        eventRepo.createMatch(ev.getId(), created);
        Console.WriteLine($"Match created with ID {created.getId()}.");
        waitForEnter();
        return created;
    }

    public bool updateEvent()
    {
        ConsolePanes.log("FTS.updateEvent()");
        ConsolePanes.clearMain();

        List<Event> events = eventRepo.getEvents();
        if (events.Count == 0)
        {
            Console.WriteLine("No events are available.");
            waitForEnter();
            return false;
        }

        foreach (Event e in events)
        {
            Console.WriteLine($"{e.getId()}. {e.getName()}");
        }

        Event? current = eventRepo.findEventById(readInt("Event ID", events[0].getId()));
        if (current == null)
        {
            Console.WriteLine("Event not found.");
            waitForEnter();
            return false;
        }

        string name = readText("New name", current.getName());
        int start = readTimestamp("New start (yyyy-MM-dd HH:mm)", current.getStartTimestamp());
        int end = readTimestamp("New end (yyyy-MM-dd HH:mm)", current.getEndTimestamp());

        Event updated = new(current.getId(), name, start, end);
        bool result = eventRepo.updateEvent(current.getId(), updated);
        Console.WriteLine(result ? "Event updated." : "Event update failed.");
        waitForEnter();
        return result;
    }

    public bool updateMatch()
    {
        ConsolePanes.log("FTS.updateMatch()");
        ConsolePanes.clearMain();

        List<Match> matches = eventRepo.getEvents().SelectMany(e => e.getChildren().OfType<Match>()).ToList();
        if (matches.Count == 0)
        {
            Console.WriteLine("No matches are available.");
            waitForEnter();
            return false;
        }

        foreach (Match m in matches)
        {
            Console.WriteLine($"{m.getId()}. {m.getName()}");
        }

        Match? current = eventRepo.findMatchById(readInt("Match ID", matches[0].getId()));
        if (current == null)
        {
            Console.WriteLine("Match not found.");
            waitForEnter();
            return false;
        }

        string name = readText("New name", current.getName());
        double coefficient = readDouble("New price coefficient", current.priceCoefficient);
        int start = readTimestamp("New start (yyyy-MM-dd HH:mm)", current.getStartTimestamp());
        int end = readTimestamp("New end (yyyy-MM-dd HH:mm)", current.getEndTimestamp());

        Match updated = new(current.getId(), name, coefficient, current.stadiumMap.clone(), start, end);
        updated.seatStatuses = new Dictionary<int, SeatStatus>(current.seatStatuses);

        bool result = eventRepo.updateMatch(current.getId(), updated);
        Console.WriteLine(result ? "Match updated." : "Match update failed.");
        waitForEnter();
        return result;
    }

    public bool updateSeat()
    {
        ConsolePanes.log("FTS.updateSeat()");
        ConsolePanes.clearMain();

        List<Match> matches = eventRepo.getEvents().SelectMany(e => e.getChildren().OfType<Match>()).ToList();
        if (matches.Count == 0)
        {
            Console.WriteLine("No matches are available.");
            waitForEnter();
            return false;
        }

        foreach (Match m in matches)
        {
            Console.WriteLine($"{m.getId()}. {m.getName()}");
        }

        Match? match = eventRepo.findMatchById(readInt("Match ID", matches[0].getId()));
        if (match == null)
        {
            Console.WriteLine("Match not found.");
            waitForEnter();
            return false;
        }

        foreach (Seat seat in match.stadiumMap.seats)
        {
            SeatStatus status = match.seatStatuses.GetValueOrDefault(seat.id, SeatStatus.Free);
            Console.WriteLine($"Seat {seat.id}: {status}");
        }

        int seatId = readInt("Seat ID", match.stadiumMap.seats.FirstOrDefault()?.id ?? 1);
        if (match.stadiumMap.seats.All(s => s.id != seatId))
        {
            Console.WriteLine("Seat not found.");
            waitForEnter();
            return false;
        }

        Console.WriteLine("Statuses: 1 - Free, 2 - Booked, 3 - Sold, 4 - Blocked");
        SeatStatus newStatus = readInt("New status", 1) switch
        {
            2 => SeatStatus.Booked,
            3 => SeatStatus.Sold,
            4 => SeatStatus.Blocked,
            _ => SeatStatus.Free,
        };

        Event? owner = eventRepo.getEvents().FirstOrDefault(e => e.findMatchById(match.getId()) != null);
        if (owner == null)
        {
            Console.WriteLine("Event for this match not found.");
            waitForEnter();
            return false;
        }

        bool result = owner.setSeatStatus(match.getId(), seatId, newStatus);
        Console.WriteLine(result ? "Seat status updated." : "Seat status is already set to this value.");
        waitForEnter();
        return result;
    }

    private static string readText(string prompt, string fallback)
    {
        Console.Write($"{prompt} [{fallback}]: ");
        string? input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? fallback : input.Trim();
    }

    private static int readInt(string prompt, int fallback)
    {
        Console.Write($"{prompt} [{fallback}]: ");
        string? input = Console.ReadLine();
        return int.TryParse(input, out int value) ? value : fallback;
    }

    private static double readDouble(string prompt, double fallback)
    {
        Console.Write($"{prompt} [{fallback:F2}]: ");
        string? input = Console.ReadLine();
        return double.TryParse(input, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double value) ? value : fallback;
    }

    private static int readTimestamp(string prompt, int fallback)
    {
        string defaultValue = DateTimeOffset.FromUnixTimeSeconds(fallback).ToString("yyyy-MM-dd HH:mm");
        Console.Write($"{prompt} [{defaultValue}]: ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            return fallback;
        }

        if (!DateTime.TryParseExact(input.Trim(),
                new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime value))
        {
            Console.WriteLine("Invalid date, using the default value.");
            return fallback;
        }

        return (int)new DateTimeOffset(value, TimeSpan.Zero).ToUnixTimeSeconds();
    }

    public void attach(AnalyticsObserver o)
    {
        ConsolePanes.log($"FTS.attach(o={o.GetType().Name})");
        reports.Add(o);
    }

    public void detach(AnalyticsObserver o)
    {
        ConsolePanes.log($"FTS.detach(o={o.GetType().Name})");
        reports.Remove(o);
    }

    public void notifyObservers(int eventId)
    {
        ConsolePanes.log($"FTS.notifyObservers(eventId={eventId})");

        Event? e = eventRepo.findEventById(eventId);
        if (e == null)
        {
            return;
        }

        HashSet<int> matchIds = e.getChildren().OfType<Match>().Select(m => m.getId()).ToHashSet();
        List<Pass> passes = passRepo.getPassesWithFilter(p => passRepo.coversAnyMatch(p, matchIds));
        int ticketsSold = passes.Count(p => !p.isRefunded);
        double revenue = passes.Where(p => !p.isRefunded).Sum(p => p.price);

        foreach (AnalyticsObserver observer in reports)
        {
            observer.update(eventId, ticketsSold, revenue);
        }
    }
}
