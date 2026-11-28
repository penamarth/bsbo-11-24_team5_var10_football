namespace FootballSystem.Passes;

using FootballSystem.Events;
using FootballSystem.UI;

public class TicketRefundStrategy : RefundStrategy
{
    public void releaseSeats(Pass pass, EventRepository eventRepo)
    {
        ConsolePanes.log($"TicketRefundStrategy.releaseSeats(passId={pass.id})");
        if (pass is Ticket ticket)
        {
            eventRepo.refundSeat(ticket.matchId, ticket.seatId);
        }
    }
}
