using FootballTicketSystem.Devices;
using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Controllers;

public sealed class EntryController
{
    private readonly FTS _fts;
    private readonly Turnstile _turnstile;
    private readonly int _matchId;
    private Pass? _pendingPass;

    public EntryController(FTS fts, Turnstile turnstile, int matchId)
    {
        _fts = fts;
        _turnstile = turnstile;
        _matchId = matchId;
    }

    public void RequestScan()
    {
        _pendingPass = null;
        _fts.RequestScan();
    }

    public Pass? ScanTicket(ScannedData scannedData)
    {
        var match = _fts.Events.FindMatchById(_matchId);

        if (match == null || !match.AcceptsEntries(_fts.Now))
        {
            _fts.Screen.Show("Проход временно невозможен, обратитесь к контролёру");
            return null;
        }

        var pass = _fts.Passes.FindPassByToken(scannedData.QrToken);

        if (pass == null)
        {
            _fts.Screen.Show("Билет недействителен, обратитесь к контролёру");
            _fts.Security.NotifyDeniedEntry($"поддельный QR-код: {scannedData.QrToken}");
            _fts.Audit.Record("Проход", "отказ: билет не найден");
            return null;
        }

        if (pass.IsRefunded)
        {
            _fts.Screen.Show("Билет был возвращён и недействителен");
            _fts.Audit.Record("Проход", $"отказ: билет #{pass.Id} возвращён");
            return null;
        }

        if (!pass.CoveredMatches.Contains(_matchId))
        {
            var other = _fts.Events.FindMatchById(pass.CoveredMatches[0]);
            _fts.Screen.Show(other == null
                ? "Билет не действует на текущий матч"
                : $"Билет действует на матч «{other.Name}» ({other.StartTimestamp:g})");
            _fts.Audit.Record("Проход", $"отказ: билет #{pass.Id} на другой матч");
            return null;
        }

        if (pass.IsUsedFor(_matchId))
        {
            var entry = pass.Entries.First(e => e.MatchId == _matchId);
            _fts.Screen.Show($"Билет уже использован: проход {entry.Timestamp:T}, турникет {entry.TurnstileId}");
            _fts.Security.NotifyDeniedEntry($"повторный проход по билету #{pass.Id}");
            _fts.Audit.Record("Проход", $"отказ: повторный проход по билету #{pass.Id}");
            return null;
        }

        var seat = match.Map?.FindSeat(pass.SeatId);
        _fts.Screen.Show(seat == null
            ? "Проход разрешён"
            : $"Ваше место: {seat.Location}");

        _fts.UnlockTurnstile(_turnstile.StadiumId, _turnstile.Id);
        _pendingPass = pass;
        _fts.Audit.Record("Проход", $"допуск подтверждён для билета #{pass.Id}");
        return pass;
    }

    public EntryRecord? PassThrough()
    {
        if (_pendingPass is not { } pass)
        {
            return null;
        }

        if (!_turnstile.PassageDetected)
        {
            _fts.LockTurnstile(_turnstile.StadiumId, _turnstile.Id);
            _fts.Screen.Show("Зритель не прошёл через турникет, проход не зарегистрирован");
            return null;
        }

        _fts.LockTurnstile(_turnstile.StadiumId, _turnstile.Id);
        var record = pass.MarkUsed(_matchId, _turnstile.Id, _fts.Now);

        var seat = _fts.Events.FindMatchById(_matchId)?.Map?.FindSeat(pass.SeatId);
        if (seat != null)
        {
            _fts.Security.NotifySeatOccupied(seat.Location, _matchId);
        }

        _fts.Server.Synchronize($"Проход по билету #{pass.Id}, турникет {_turnstile.Id}");
        _fts.Audit.Record("Проход", $"успешный проход по билету #{pass.Id}, турникет {_turnstile.Id}");
        _fts.Screen.Show("Проход зарегистрирован. Турникет в режиме ожидания");
        _pendingPass = null;
        return record;
    }
}
