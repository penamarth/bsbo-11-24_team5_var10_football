namespace FootballSystem.Models;

using FootballSystem.Storages;
using FootballSystem.Banking;
using FootballSystem.Enums;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class FTS
{
		private Storages.PassStorage _passStorage;
		private Storages.EventStorage _eventStorage;
		private Banking.IBankAdapter _bank;
		private List<Report> _reports;

		public FTS(Storages.PassStorage passStorage, Storages.EventStorage eventStorage, Banking.IBankAdapter bank)
		{
				_passStorage = passStorage;
				_eventStorage = eventStorage;
				_bank = bank;
		}

		public async Task Run()
		{
				_passStorage.CreatePass(new Ticket(0, 100));

				string msg = "1-5";
				for(;;)
				{
						Console.Write("\x1b[2J\x1b[H");

						Console.WriteLine("1. Use a ticket");
						Console.WriteLine("2. Buy a ticket");
						Console.WriteLine("3. Refund a ticket");
						Console.WriteLine("4. View analytics");
						Console.WriteLine("5. Manage events");

						Console.Write($"{msg}> ");
						msg = "1-5";

						string ans = Console.ReadLine();
						switch(ans)
						{
								case "1":
										break;
								case "2":
										await StartPurchase();
										break;
								case "3":
										await StartRefund();
										break;
								case "4":
										break;
								case "5":
										break;
								default:
										msg = "Only digits between 1-5";
										break;
						}
				}
		}

		private const int BookingTimeoutSeconds = 300;

		public async Task StartPurchase()
		{
				// Шаги 1-3: список мероприятий, открытых для продажи
				List<Event> events = GetAvailableEvents();
				if (events.Count == 0)
				{
						Console.WriteLine("No events are open for sale right now.");
						Pause();
						return;
				}

				Event? ev = SelectEvent(events);
				if (ev == null) return;

				// Шаг 4: билет на матч или абонемент на сезон
				bool? isSubscription = RequestPurchaseType();
				if (isSubscription == null) return;

				List<Match> seasonMatches;
				Match? match;

				if (isSubscription.Value)
				{
						int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
						seasonMatches = ev.Matches.Where(m => m.EndUnixTimestamp > now).ToList();
						if (seasonMatches.Count == 0)
						{
								Console.WriteLine("No matches left in this season.");
								Pause();
								return;
						}
						match = seasonMatches.First();
				}
				else
				{
						match = SelectMatch(ev);
						if (match == null) return;
						seasonMatches = new List<Match> { match };
				}

				// Шаги 5-8: схема стадиона и бронь места на время оплаты (8а: место занято — схема обновляется)
				StadiumMap? map = GetSeatMap(match.ID);
				if (map == null)
				{
						Console.WriteLine("Seat map is not available.");
						Pause();
						return;
				}

				string seatMapTitle = isSubscription.Value
						? $"subscription for {ev.Name}"
						: match.Name;

				int? seatId;
				for(;;)
				{
						seatId = SelectSeat(seatMapTitle, map, match);
						if (seatId == null) return;

						if (ReserveSeats(seasonMatches, seatId.Value)) break;

						ReleaseSeats(seasonMatches, seatId.Value);
						Console.WriteLine("This seat is already taken. The seat map has been updated.");
				}

				Seat? seat = map.Seats.FirstOrDefault(s => s.ID == seatId.Value);
				if (seat == null)
				{
						ReleaseSeats(seasonMatches, seatId.Value);
						return;
				}

				double price = CalculatePrice(seat, seasonMatches);

				// Шаги 9-10: покупателю показаны матч/абонемент, место и стоимость
				if (!ShowPurchaseDetails(ev, match, seat, price, isSubscription.Value, seasonMatches.Count))
				{
						ReleaseSeats(seasonMatches, seatId.Value);
						Console.WriteLine("Purchase cancelled.");
						return;
				}

				// Шаги 11-12: адрес электронной почты (12а: некорректный адрес вводится повторно)
				string? email = RequestEmail();
				if (email == null)
				{
						ReleaseSeats(seasonMatches, seatId.Value);
						Console.WriteLine("Purchase cancelled.");
						return;
				}

				// Шаги 13-17: оплата (16а: банк недоступен, 17а: платёж отклонён, 14а: бронь истекла)
				string description = isSubscription.Value
						? $"FTS subscription: {ev.Name}, seat {seat.ID}"
						: $"FTS ticket: {ev.Name} / {match.Name}, seat {seat.ID}";

				string? credentials = null;
				while (credentials == null)
				{
						if (!CheckBooking(seasonMatches, seatId.Value))
						{
								Console.WriteLine("The seat reservation has expired, the purchase is cancelled.");
								Pause();
								return;
						}

						credentials = await PayByCard(description, price);

						if (credentials == null)
						{
								Console.WriteLine("Do you wish to retry the payment (y/N)?");
								if (Console.ReadLine() != "y")
								{
										ReleaseSeats(seasonMatches, seatId.Value);
										Console.WriteLine("Purchase cancelled.");
										return;
								}
						}
				}

				// Шаги 18-19: создаётся Ticket/Subscription с уникальным QR-кодом и сохраняется в PassStorage,
				// место помечается как проданное (для абонемента — во всех матчах сезона)
				Pass pass = isSubscription.Value
						? BuildSubscription(ev, seasonMatches)
						: new Ticket(match.ID, price);

				pass.SeatID = seat.ID;
				pass.Price = price;
				pass.Email = email;
				pass.PaymentMethod = PaymentType.Card;
				pass.PaymentCredentials = credentials;
				_passStorage.CreatePass(pass);
				pass.QRCode = GenerateQRCode(pass.ID);

				foreach (Match m in seasonMatches)
						ev.SetSeatStatus(m.ID, seatId.Value, SeatStatus.Sold);

				// Шаг 20: покупателю показывается страница подтверждения с QR-кодом
				ShowPurchaseConfirmation(pass);

				// Шаг 21: письмо с электронной копией билета/абонемента и электронным чеком
				await SendTicketEmail(pass);

				Pause();
		}

		public Event? SelectEvent(List<Event> events)
		{
				for(;;)
				{
						Console.Write("\x1b[2J\x1b[H");
						Console.WriteLine("===== EVENTS OPEN FOR SALE =====");
						for (int i = 0; i < events.Count; i++)
						{
								Console.WriteLine($"{i + 1}. {events[i].Name} ({FormatDate(events[i].StartUnixTimestamp)} - {FormatDate(events[i].EndUnixTimestamp)})");
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

		public Match? SelectMatch(Event ev)
		{
				int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				List<Match> openMatches = ev.Matches.Where(m => m.EndUnixTimestamp > now).ToList();
				if (openMatches.Count == 0)
				{
						Console.WriteLine("No matches of this event are open for sale.");
						Pause();
						return null;
				}

				for(;;)
				{
						Console.Write("\x1b[2J\x1b[H");
						Console.WriteLine($"===== MATCHES: {ev.Name} =====");
						for (int i = 0; i < openMatches.Count; i++)
						{
								Console.WriteLine($"{i + 1}. {openMatches[i].Name} ({FormatDate(openMatches[i].StartUnixTimestamp)})");
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

		public int? SelectSeat(string title, StadiumMap map, Match match)
		{
				for(;;)
				{
						Console.Write("\x1b[2J\x1b[H");
						Console.WriteLine($"===== SEAT MAP: {map.Name} ({title}) =====");
						foreach (Seat s in map.Seats)
						{
								string status = match.GetSeatStatus(s.ID) switch
								{
										SeatStatus.Free => "free",
										SeatStatus.Booked => "booked",
										SeatStatus.Sold => "sold",
										_ => "blocked",
								};
								Console.WriteLine($"Seat {s.ID,3} | sector {s.Sector}, row {s.Row}, place {s.Column} | {s.Price,8:F2} | {status}");
						}
						Console.WriteLine("================================");

						Console.Write("Enter a seat ID to pick (or press Enter to cancel): ");
						string? input = Console.ReadLine();
						if (string.IsNullOrWhiteSpace(input)) return null;

						if (int.TryParse(input, out int seatId) && map.Seats.Any(s => s.ID == seatId))
						{
								return seatId;
						}

						Console.WriteLine("Invalid seat ID.");
				}
		}

		public bool? RequestPurchaseType()
		{
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

		public bool ShowPurchaseDetails(Event ev, Match match, Seat seat, double price, bool isSubscription, int matchCount)
		{
				Console.Write("\x1b[2J\x1b[H");
				Console.WriteLine("===== PURCHASE DETAILS =====");
				Console.WriteLine($"Event:          {ev.Name}");
				Console.WriteLine(isSubscription
						? $"Subscription:   all {matchCount} matches of the season"
						: $"Match:          {match.Name} ({FormatDate(match.StartUnixTimestamp)})");
				Console.WriteLine($"Seat:           {seat.ID} (sector {seat.Sector}, row {seat.Row}, place {seat.Column})");
				Console.WriteLine($"Total price:    {price:F2}");
				Console.WriteLine("============================");
				Console.Write("Proceed with the purchase (y/N)? ");
				return Console.ReadLine() == "y";
		}

		public string? RequestEmail()
		{
				for(;;)
				{
						Console.Write("Enter your email for the e-pass and the receipt (or press Enter to cancel): ");
						string? input = Console.ReadLine();
						if (string.IsNullOrWhiteSpace(input)) return null;

						string email = input.Trim();
						if (IsValidEmail(email)) return email;

						Console.WriteLine("The email address is invalid, please try again.");
				}
		}

		public async Task<string?> PayByCard(string description, double amount)
		{
				string? url;
				try
				{
						url = await _bank.StartPurchase(description, amount);
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
				string status = await _bank.ConfirmPurchase(url);

				if (status != "success")
				{
						Console.WriteLine("The bank declined the payment.");
						return null;
				}

				return TransactionID(url);
		}

		public async Task SendTicketEmail(Pass pass)
		{
				string text = PurchaseText(pass) + "\n" + ReceiptText(pass);
				await SendEmail(pass.Email, text);
		}

		public void ShowPurchaseConfirmation(Pass pass)
		{
				Console.WriteLine("===== PURCHASE CONFIRMED =====");
				Console.WriteLine(PurchaseText(pass));
				Console.WriteLine("==============================");
		}

		private Subscription BuildSubscription(Event ev, List<Match> seasonMatches)
		{
				Subscription subscription = new(seasonMatches.Select(m => m.ID).ToHashSet());
				subscription.StartUnixTimestamp = ev.StartUnixTimestamp;
				subscription.EndUnixTimestamp = ev.EndUnixTimestamp;
				return subscription;
		}

		private bool ReserveSeats(List<Match> matches, int seatId)
		{
				foreach (Match match in matches)
				{
						if (!_eventStorage.ReserveSeat(match.ID, seatId, BookingTimeoutSeconds))
						{
								return false;
						}
				}
				return true;
		}

		private void ReleaseSeats(List<Match> matches, int seatId)
		{
				foreach (Match match in matches)
				{
						_eventStorage.ReleaseSeat(match.ID, seatId);
				}
		}

		private bool CheckBooking(List<Match> matches, int seatId)
		{
				foreach (Match match in matches)
				{
						if (!_eventStorage.CheckBooking(match.ID, seatId))
						{
								return false;
						}
				}
				return true;
		}

		private static double CalculatePrice(Seat seat, List<Match> matches)
		{
				return matches.Sum(m => seat.Price * m.PriceCoefficient);
		}

		private static string TransactionID(string url)
		{
				string id = url.TrimEnd('/').Split('/').Last();
				return string.IsNullOrEmpty(id) ? url : id;
		}

		private static string GenerateQRCode(int passId)
		{
				return $"FTS-{passId}-{Guid.NewGuid():N}";
		}

		private static string PurchaseText(Pass pass)
		{
				string text = "Electronic pass\n" +
						$"Pass ID:    {pass.ID}\n" +
						$"Type:       {pass.GetType().Name}\n" +
						$"Seat ID:    {pass.SeatID}\n" +
						$"Price:      {pass.Price:F2}\n" +
						$"QR code:    {pass.QRCode}";

				if (pass is Ticket ticket) text += $"\nMatch ID:   {ticket.MatchID}";
				if (pass is Subscription subscription) text += $"\nSeason:     {FormatDate(subscription.StartUnixTimestamp)} - {FormatDate(subscription.EndUnixTimestamp)}";

				return text;
		}

		private static string ReceiptText(Pass pass)
		{
				return "E-RECEIPT\n" +
						$"Pass ID:         {pass.ID}\n" +
						$"Paid:            {pass.Price:F2}\n" +
						$"Payment method:  {pass.PaymentMethod}";
		}

		private static bool IsValidEmail(string email)
		{
				int at = email.IndexOf('@');
				return at > 0
						&& email.IndexOf('@', at + 1) == -1
						&& email.IndexOf('.', at + 1) > at + 1
						&& email[^1] != '.';
		}

		private static string FormatDate(int unixTimestamp)
		{
				return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).ToString("dd.MM.yyyy HH:mm");
		}

		private static void Pause()
		{
				Console.WriteLine("Press Enter to continue...");
				Console.ReadLine();
		}

		public async Task StartRefund()
		{
				int? passId = RequestScan();
				if (passId == null) return;

				bool refundAllowed = _passStorage.CheckRefundEligibility(passId.Value);
				while (!refundAllowed)
				{
						Console.WriteLine("Refund isn't allowed, do you wish to scan some other pass (y/N)?");
						if (Console.ReadLine() != "y") return;
						passId = RequestScan();
						if (passId == null) return;
						refundAllowed = _passStorage.CheckRefundEligibility(passId.Value);
				}

				Pass? pass = _passStorage.GetPassByID(passId.Value);
				if (pass == null) { Console.WriteLine("Pass not found."); return; }
				if (pass.Price <= 0) { Console.WriteLine("No refundable amount on this pass."); return; }

				if (!ShowRefundDetails(pass)) { Console.WriteLine("Refund cancelled."); return; }

				bool refunded;
				switch (pass.PaymentMethod)
				{
						case PaymentType.Card:
								refunded = await RefundToCard(pass);
								if (!refunded)
								{
										Console.WriteLine("Failed to refund to card. Do you wish to receive it in cash (y/N)?");
										if (Console.ReadLine() != "y") return;
										refunded = await RefundWithCash(pass);
								}
								break;

						case PaymentType.Cash:
								refunded = await RefundWithCash(pass);
								break;

						default:
								Console.WriteLine("Unknown payment method. Do you wish to receive the refund in cash (y/N)?");
								if (Console.ReadLine() != "y") return;
								refunded = await RefundWithCash(pass);
								break;
				}

				if (!refunded) { Console.WriteLine("Refund failed."); return; }

				if (!_passStorage.Refund(pass.ID))
						Console.WriteLine("Warning: this pass was already marked as refunded.");

				await GiveReceipt(pass);
		}

		public bool ShowRefundDetails(Pass pass)
		{
				Console.Write("\x1b[2J\x1b[H");
				Console.WriteLine("===== REFUND DETAILS =====");
				Console.WriteLine($"Pass ID:          {pass.ID}");
				Console.WriteLine($"Type:             {pass.GetType().Name}");
				Console.WriteLine($"Seat ID:          {pass.SeatID}");
				Console.WriteLine($"Refund amount:    {pass.Price:F2}");
				Console.WriteLine($"Payment method:   {pass.PaymentMethod}");
				Console.WriteLine($"Already refunded: {pass.IsRefunded}");
				Console.WriteLine("==========================");
				Console.Write("Proceed with the refund (y/N)? ");
				return Console.ReadLine() == "y";
		}

		public async Task<bool> RefundToCard(Pass pass)
		{
				if (string.IsNullOrEmpty(pass.PaymentCredentials))
				{
						Console.WriteLine("No card credentials on this pass.");
						return false;
				}

				Console.WriteLine($"Refunding {pass.Price:F2} to card...");
				try { return await _bank.Refund(pass.PaymentCredentials, "", pass.Price); }
				catch (Exception ex) { Console.WriteLine($"Card refund error: {ex.Message}"); return false; }
		}

		public async Task<bool> RefundWithCash(Pass pass)
		{
				Console.WriteLine($"Please hand {pass.Price:F2} to the customer.");
				await Task.Delay(500);
				Console.WriteLine("Cash refund completed.");
				return true;
		}

		public int? RequestScan()
		{
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

		public async Task GiveReceipt(Pass pass)
		{
				string text = "Ticket for a refund\n" +
						$"Refunded: {pass.Price:F2}";
				await Task.WhenAll(SendEmail(pass.Email, text), PrintReceipt(text));
		}

		private async Task<bool> PrintReceipt(string text)
		{
				Console.WriteLine("Printing a receipt...");
				await Task.Delay(2000);
				Console.WriteLine("===== RECEIPT =====");
				Console.WriteLine(text);
				Console.WriteLine("===================");
				return true;
		}

		private async Task<bool> SendEmail(string email, string text)
		{
				Console.WriteLine($"Sending an email to {email}...");
				await Task.Delay(1000);
				Console.WriteLine("====== EMAIL ======");
				Console.WriteLine(text);
				Console.WriteLine("===================");
				return true;
		}

		public Report? GetReport(int eventId)
		{
				return _reports.FirstOrDefault(r => r.EventID == eventId);
		}

		public List<Event> GetAvailableEvents()
		{
				// открыты для продажи мероприятия, которые ещё не закончились
				return _eventStorage.GetEvents().Where(e => e.EndUnixTimestamp > DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToList();
		}

		public StadiumMap? GetSeatMap(int matchId)
		{
				Match? match = _eventStorage.FindMatchByID(matchId);
				if (match == null)
				{
						return null;
				}
				return match.StadiumMap;
		}

		public bool UnlockTurnstile(int turnstileId)
		{
				return true;
		}

		public bool LockTurnstile(int turnstileId)
		{
				return true;
		}
}
