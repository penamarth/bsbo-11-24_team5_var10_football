namespace FootballTicketSystem.Domain;

public sealed class EmailAddress
{
    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static bool IsValid(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Split('@');
        return parts.Length == 2
            && parts[0].Length > 0
            && parts[1].Contains('.')
            && !parts[1].StartsWith('.')
            && !parts[1].EndsWith('.')
            && !input.Any(char.IsWhiteSpace);
    }

    public static bool TryParse(string? input, out EmailAddress? email)
    {
        if (IsValid(input))
        {
            email = new EmailAddress(input!);
            return true;
        }

        email = null;
        return false;
    }

    public static EmailAddress Parse(string? input)
    {
        if (!TryParse(input, out var email))
        {
            throw new FormatException($"Некорректный адрес электронной почты: {input}");
        }

        return email!;
    }

    public override string ToString() => Value;
}
