using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Controllers;

public sealed class RefundController
{
    private readonly FTS _fts;
    private Pass? _pendingPass;

    public RefundController(FTS fts) => _fts = fts;

    public void StartRefund()
    {
        _pendingPass = null;
        _fts.Screen.Show("Режим возврата билета/абонемента");
        _fts.RequestScan();
    }

    public bool ScanTicket(ScannedData scannedData)
    {
        var pass = _fts.Passes.FindPassByToken(scannedData.QrToken);

        if (pass == null)
        {
            _fts.Screen.Show("Код не распознан, билет не найден. Отсканируйте повторно или обратитесь к сотруднику");
            return false;
        }

        if (!_fts.Passes.CheckRefundEligibility(pass.Id))
        {
            _fts.Screen.Show($"Билет/абонемент #{pass.Id} уже использован или возвращён");
            return false;
        }

        if (_fts.Events.HasStarted(pass.CoveredMatches, _fts.Now))
        {
            _fts.Screen.Show($"По билету/абонементу #{pass.Id} уже были матчи, возврат невозможен");
            return false;
        }

        _pendingPass = pass;
        var amount = _fts.PriceOf(pass);
        _fts.Screen.Show($"Доступно к возврату: {amount}. Условия: деньги возвращаются на способ оплаты, билет становится недействительным");
        return true;
    }

    public Refund? ConfirmRefund()
    {
        if (_pendingPass is not { } pass)
        {
            _fts.Screen.Show("Сначала отсканируйте билет/абонемент");
            return null;
        }

        if (!_fts.Passes.CheckRefundEligibility(pass.Id))
        {
            _fts.Screen.Show("Билет/абонемент не может быть возвращён");
            return null;
        }

        var refund = _fts.CreateRefund(pass.Id);
        if (refund == null)
        {
            _fts.Screen.Show("Не удалось создать запрос на возврат");
            return null;
        }

        if (pass.PaymentMethod == PaymentType.Card && _fts.Bank.StartRefund(pass.PaymentCredentials, refund.Amount))
        {
            refund.MarkReturnedToCard();
            _fts.Screen.Show($"Средства возвращены на карту, заказ {pass.PaymentCredentials}");
        }
        else
        {
            _fts.Cash.PayOut(refund.Amount);
            refund.MarkPaidInCash();
            _fts.Screen.Show($"Средства выданы наличными: {refund.Amount}");
        }

        _fts.Printer.PrintReceipt($"Возврат билета/абонемента #{pass.Id}: {refund.Amount}");
        _fts.Server.Synchronize($"Возврат билета/абонемента #{pass.Id}");
        _fts.Audit.Record("Возврат", $"Билет/абонемент #{pass.Id} возвращён, сумма {refund.Amount}");
        _pendingPass = null;
        _fts.Screen.Show("Начальный экран");
        return refund;
    }

    public void Cancel()
    {
        _pendingPass = null;
        _fts.Screen.Show("Начальный экран");
    }
}
