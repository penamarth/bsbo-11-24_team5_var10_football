namespace FootballTicketSystem.Domain;

public sealed record Payment(Money Amount, PaymentType Method, string? Reference);
