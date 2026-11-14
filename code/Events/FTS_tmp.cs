namespace FootballSystem.Events;

public async Task StartEventCreation() {
    List<Event> eventsList= _eventStorage.getEvents();
    for (;;) {
        EventsCMD(eventsList);
        string opt = Console.ReadLine();
        switch (opt)
        {
            case "1":
                int id = _eventStorage.lastId();
                Console.WriteLine("(Write Event Name)> ");
                string name = Console.ReadLine();
                Console.WriteLine("(Write Event StartDate)> ");
                string start = Console.ReadLine();
                if (DateTimeOffset.TryParseExact(input, "dd.MM.yyyy HH:mm", DateTimeStyles.AssumeLocal, out DateTimeOffset startTime))
                    int startDate = startTime.ToUnixTimeSeconds();
                Console.WriteLine("(Write Event EndDate)> ");
                string end = Console.ReadLine();
                if (DateTimeOffset.TryParseExact(input, "dd.MM.yyyy HH:mm", DateTimeStyles.AssumeLocal, out DateTimeOffset endTime))
                    int endDate = startTime.ToUnixTimeSeconds();
                Event newEvent = Event(id, name, (startDate, endDate));
                _eventStorage.addEvent(newEvent);
            case "2":
                Console.WriteLine("(Select Event With Id)> ");
                int id = Convert.ToInt32(Console.ReadLine());
                Event e = _eventStorage.findEventById(id);
                int lastMatchId = e.lastId();
                Match match = new Match()
            case "0":
                break;
            default:
                continue;
        }
    }
}
void EventsCMD(List<Event> eventsList) {
    Console.Write("\x1b[2J\x1b[H");
    Console.WriteLine("===== EVENTS LIST =====");
    if (eventsList)
        Console.WriteLine("There is no events yet...");
    else {
        foreach (Event e in eventsList) {
            Console.WriteLine($"{e.getId()}. " + e.name);
            count++;
        }
    }
    Console.WriteLine("=================");
    Console.WriteLine("Choose your option:");
    Console.WriteLine("1. Create new event");
    Console.WriteLine("2. Redact exisiting event");
    Console.WriteLine("=================");
    Console.WriteLine("0. Exit");
    Console.Write("\n> ")
}