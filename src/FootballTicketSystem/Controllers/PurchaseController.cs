using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Controllers;

public sealed class PurchaseController
{
    private static readonly TimeSpan ReservationTimeout = TimeSpan.FromMinutes(10);

    private readonly FTS _fts;

    public PurchaseController(FTS fts) => _fts = fts;

    public Purchase? Current { get; private set; }

    public Purchase StartPurchase()
    {
        Current = _fts.CreatePurchase();
        _fts.Screen.Show("Покупка билета/абонемента. Доступные события:");

        foreach (var e in _fts.GetAvailableEvents())
        {
            var minPrice = e.Map == null
                ? Money.Zero
                : new Money(e.Map.Seats.Select(s => s.Price.Amount).Min()) * (decimal)e.PriceCoefficient;
            _fts.Screen.Show($"  {e.Id}. {e.Name} — матчей: {e.Matches.Count}, от {minPrice}");
        }

        return Current;
    }

    public StadiumMap? SelectEvent(int eventId)
    {
        var purchase = Current ?? throw new InvalidOperationException("Покупка не начата");
        var e = _fts.Events.FindEventById(eventId);

        if (e == null || !e.OnSale)
        {
            _fts.Screen.Show("Продажа билетов на это мероприятие открыта не была или уже закрыта");
            return null;
        }

        purchase.Event = e;
        var map = _fts.GetSeatMap(eventId);
        _fts.Screen.Show($"Мероприятие «{e.Name}». Свободные места:");

        foreach (var seat in _fts.Events.AvailableSeats(e))
        {
            _fts.Screen.Show($"  {seat.Location} — {e.CalculatePrice(seat)}");
        }

        return map;
    }

    public bool SelectSeat(int seatId)
    {
        var purchase = Current ?? throw new InvalidOperationException("Покупка не начата");

        if (purchase.Event == null)
        {
            _fts.Screen.Show("Сначала выберите мероприятие");
            return false;
        }

        var reservation = _fts.Events.ReserveSeat(purchase.Event, seatId, ReservationTimeout, _fts.Now);
        if (reservation == null)
        {
            _fts.Screen.Show("Место уже занято. Начните покупку заново");
            Current = null;
            return false;
        }

        var seat = purchase.Event.Map!.FindSeat(seatId)!;
        purchase.Reservation = reservation;
        purchase.Seat = seat;
        purchase.Price = purchase.Event.CalculatePrice(seat);
        _fts.Screen.Show($"Мероприятие «{purchase.Event.Name}», {seat.Location}, стоимость {purchase.Price}. Подтвердите покупку");
        return true;
    }

    public void ConfirmPurchase(PaymentType paymentType)
    {
        var purchase = Current ?? throw new InvalidOperationException("Покупка не начата");
        purchase.PaymentType = paymentType;
        _fts.Screen.Show("Введите адрес электронной почты для получения электронной копии");
    }

    public bool EnterEmail(string input)
    {
        var purchase = Current ?? throw new InvalidOperationException("Покупка не начата");

        if (!EmailAddress.TryParse(input, out var email))
        {
            _fts.Screen.Show("Адрес электронной почты введён неверно. Введите адрес ещё раз");
            return false;
        }

        purchase.Email = email;
        _fts.Screen.Show($"Внесите оплату: {purchase.Price}");
        return true;
    }

    public Pass? MakePayment(Money amount)
    {
        var purchase = Current ?? throw new InvalidOperationException("Покупка не начата");

        if (purchase.Event == null || purchase.Seat == null || purchase.Reservation == null || purchase.Email == null)
        {
            _fts.Screen.Show("Покупка не готова к оплате: выберите мероприятие, место и введите почту");
            return null;
        }

        if (!_fts.Events.IsReservationActive(purchase.Reservation, _fts.Now))
        {
            _fts.Events.ReleaseReservation(purchase.Reservation);
            _fts.Screen.Show("Время на оплату истекло, бронь снята");
            Cancel();
            return null;
        }

        var price = purchase.Price;

        if (purchase.PaymentType == PaymentType.Card)
        {
            var orderToken = _fts.Bank.StartPurchase(price);

            if (orderToken == string.Empty || !_fts.Bank.ChargeCard(orderToken))
            {
                _fts.Screen.Show("Не удалось списать средства с карты. Попробуйте оплатить наличными");
                return null;
            }

            purchase.Payment = new Payment(price, PaymentType.Card, orderToken);
        }
        else
        {
            if (!_fts.Cash.TryAcceptPayment(price, amount, out var change))
            {
                _fts.Screen.Show("Внесено недостаточно средств");
                return null;
            }

            purchase.Payment = new Payment(price, PaymentType.Cash, "CASH");
            _fts.Screen.Show($"Оплата принята. Сдача: {change}");
        }

        _fts.Events.ConfirmReservation(purchase.Reservation);
        var pass = _fts.CreatePass(purchase);
        purchase.Pass = pass;

        _fts.Printer.PrintTicket(pass);
        _fts.Printer.PrintReceipt($"Покупка билета/абонемента #{pass.Id}, сумма {price}");
        _fts.SendTicketEmail(purchase.Email, pass);
        _fts.Server.Synchronize($"Продажа билета/абонемента #{pass.Id}");
        _fts.Audit.Record("Покупка", $"Билет/абонемент #{pass.Id}, место {purchase.Seat.Location}, сумма {price}");
        _fts.Screen.Show("Покупка завершена");
        Current = null;
        return pass;
    }

    public void Cancel()
    {
        if (Current?.Reservation != null)
        {
            _fts.Events.ReleaseReservation(Current.Reservation);
        }

        Current = null;
        _fts.Screen.Show("Начальный экран");
    }
}
