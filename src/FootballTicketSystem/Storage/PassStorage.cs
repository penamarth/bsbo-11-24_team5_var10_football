using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Storage;

public sealed class PassStorage
{
    private readonly List<Pass> _passes = new();
    private readonly List<Refund> _refunds = new();
    private int _nextPassId = 1;

    public IReadOnlyList<Pass> Passes => _passes;

    public IReadOnlyList<Refund> Refunds => _refunds;

    public Pass AddPass(Pass pass)
    {
        if (pass.Id == 0)
        {
            pass.Id = _nextPassId++;
        }

        _passes.Add(pass);
        return pass;
    }

    public Refund AddRefund(Refund refund)
    {
        _refunds.Add(refund);
        return refund;
    }

    public Pass? FindPassById(int passId) => _passes.FirstOrDefault(p => p.Id == passId);

    public Pass? FindPassByToken(string qrToken) => _passes.FirstOrDefault(p => p.QrToken == qrToken);

    public IReadOnlyList<Pass> FindPassesByMatch(int matchId) =>
        _passes.Where(p => p.CoveredMatches.Contains(matchId)).ToList();

    public IReadOnlyList<Pass> FindPassesByEvent(Event e)
    {
        var matchIds = e.Matches.Select(m => m.Id).ToHashSet();
        return _passes.Where(p => p.CoveredMatches.Any(matchIds.Contains)).ToList();
    }

    public bool HasSales(Event e) => FindPassesByEvent(e).Count > 0;

    public bool CheckRefundEligibility(int passId) =>
        FindPassById(passId) is { IsRefunded: false } pass && pass.Entries.Count == 0;

    public bool RefundPass(int passId)
    {
        if (!CheckRefundEligibility(passId))
        {
            return false;
        }

        FindPassById(passId)!.Refund();
        return true;
    }
}
