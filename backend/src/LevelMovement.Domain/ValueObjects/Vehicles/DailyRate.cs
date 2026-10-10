namespace LevelMovement.Domain.ValueObjects.Vehicles;

public sealed record DailyRate
{
    public decimal Amount { get; }

    public DailyRate(decimal amount)
    {
        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
    }
}
