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
				return _eventStorage.GetEvents().Where(e => e.EndUnixTimestamp < DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToList();
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
