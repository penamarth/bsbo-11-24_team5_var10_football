using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Controllers;

public sealed class EventController
{
    private readonly FTS _fts;
    private Event? _current;
    private StadiumMap? _map;

    public EventController(FTS fts) => _fts = fts;

    public IReadOnlyList<Event> EventsList { get; private set; } = Array.Empty<Event>();

    public IReadOnlyList<Event> ManageEvent()
    {
        EventsList = _fts.Events.LoadEvents();
        _fts.Screen.Show("Управление мероприятиями. Кнопки: «Создать», «Редактировать»");

        foreach (var e in EventsList)
        {
            _fts.Screen.Show($"  {e.Id}. {e.Name}");
        }

        return EventsList;
    }

    public Event? CreateNewEvent(string eventName)
    {
        if (_fts.Events.FindEventByName(eventName) != null)
        {
            _fts.Screen.Show($"Мероприятие «{eventName}» уже существует");
            return null;
        }

        var now = _fts.Now;
        _map = StadiumMap.CreateStandard("Арена");
        _current = new Event(0, eventName, now, now.AddMonths(3));
        _current.CreateMatch(new Match(0, 0, eventName, _map.Name, now.AddDays(7), now.AddDays(7).AddHours(2))
        {
            Map = _map
        });

        _fts.Screen.Show($"Мероприятие «{eventName}» создано, загружена стандартная схема стадиона");
        ShowSeatMap();
        return _current;
    }

    public Event? SelectEvent(string eventName)
    {
        var e = _fts.Events.FindEventByName(eventName);

        if (e == null)
        {
            _fts.Screen.Show("Мероприятие не найдено");
            return null;
        }

        _current = e;
        _map = e.Map;
        _fts.Screen.Show($"Мероприятие «{e.Name}» открыто для редактирования");
        ShowSeatMap();
        return e;
    }

    public bool ConfigureSeat(int seatId, SeatStatus status)
    {
        if (_map == null)
        {
            _fts.Screen.Show("Карта мест не загружена");
            return false;
        }

        var seat = _map.FindSeat(seatId);

        if (seat == null)
        {
            _fts.Screen.Show($"Место {seatId} не найдено на карте стадиона");
            return false;
        }

        seat.Status = status;
        _fts.Screen.Show($"Место {seat.Location}: статус {seat.Status}");
        return true;
    }

    public bool SaveEvent()
    {
        if (_current == null)
        {
            _fts.Screen.Show("Нет мероприятия для сохранения");
            return false;
        }

        var saved = _current.Id == 0
            ? _fts.Events.CreateEvent(_current)
            : _fts.Events.UpdateEvent(_current.Id, _current);

        if (!saved)
        {
            _fts.Screen.Show("Ошибка сохранения в хранилище. Повторите операцию");
            return false;
        }

        EventsList = _fts.Events.LoadEvents();
        _fts.Audit.Record("Управление мероприятиями", $"Мероприятие «{_current.Name}» сохранено");
        _fts.Server.Synchronize($"Мероприятие {_current.Id}");
        _fts.Screen.Show($"Подтверждение: мероприятие «{_current.Name}» сохранено");
        return true;
    }

    public void CancelEditing()
    {
        _current = null;
        _map = null;
        _fts.Screen.Show("Несохранённые изменения отменены");
    }

    private void ShowSeatMap()
    {
        if (_map == null)
        {
            return;
        }

        foreach (var seat in _map.Seats)
        {
            _fts.Screen.Show($"  {seat.Location} — {seat.Price} — {seat.Status}");
        }
    }
}
