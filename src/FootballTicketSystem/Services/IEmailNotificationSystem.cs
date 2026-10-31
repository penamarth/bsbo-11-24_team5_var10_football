using FootballTicketSystem.Domain;

namespace FootballTicketSystem.Services;

public interface IEmailNotificationSystem
{
    bool SendEmail(EmailAddress to, string subject, string body);
}
