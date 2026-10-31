using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Devices;

public sealed class QrScanner
{
    public ScannedData? Scan(string qrCode) =>
        string.IsNullOrWhiteSpace(qrCode) ? null : new ScannedData(qrCode.Trim());
}
