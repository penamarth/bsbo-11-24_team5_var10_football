namespace FootballTicketSystem.Domain;

public sealed class Purchase
{
    public Event? Event { get; internal set; }

    public Seat? Seat { get; internal set; }

    public Reservation? Reservation { get; internal set; }

    public Money Price { get; internal set; }

    public EmailAddress? Email { get; internal set; }

    public PaymentType PaymentType { get; internal set; }

    public Payment? Payment { get; internal set; }

    public Pass? Pass { get; internal set; }
}
