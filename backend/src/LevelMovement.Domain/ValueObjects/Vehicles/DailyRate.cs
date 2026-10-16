namespace LevelMovement.Domain.ValueObjects.Vehicles;

public sealed record DailyRate
{
    public decimal Amount { get; }

    public DailyRate(decimal amount)
    {
        var rounded = decimal.Round(amount, 2, MidpointRounding.ToEven);

        if (rounded <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount), "Daily rate must be greater than zero.");

        if (rounded > 9999999999999999.99m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Daily rate exceeds database precision.");
        Amount = rounded;
    }
}
