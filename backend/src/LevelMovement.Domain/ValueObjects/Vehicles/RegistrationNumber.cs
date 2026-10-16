namespace LevelMovement.Domain.ValueObjects.Vehicles;

public sealed record RegistrationNumber
{
    public const int MaxLength = 20;

    public string Value { get; }

    public RegistrationNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = Normalize(value);
        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Registration number must not exceed {MaxLength} characters.", nameof(value));
        }

        Value = normalized;
    }

    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return string.Concat(value.Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();
    }
}
