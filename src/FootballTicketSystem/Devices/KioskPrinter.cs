using FootballTicketSystem.Domain;
using FootballTicketSystem.Presentation;

namespace FootballTicketSystem.Devices;

public sealed class KioskPrinter
{
    private readonly Screen _screen;

    public KioskPrinter(Screen screen) => _screen = screen;

    public void PrintTicket(Pass pass) => _screen.Show($"Билет/абонемент #{pass.Id} распечатан. QR: {pass.QrToken}");

    public void PrintReceipt(string text) => _screen.Show($"Чек: {text}");
}
