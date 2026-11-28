namespace FootballSystem.Passes;

using FootballSystem.Events;
using FootballSystem.UI;

public class SubscriptionRefundStrategy : RefundStrategy
{
    public void releaseSeats(Pass pass, EventRepository eventRepo)
    {
        ConsolePanes.log($"SubscriptionRefundStrategy.releaseSeats(passId={pass.id})");
        if (pass is Subscription subscription)
        {
            foreach (int id in subscription.matchIds)
            {
                eventRepo.refundSeat(id, subscription.seatId);
            }
        }
    }
}
