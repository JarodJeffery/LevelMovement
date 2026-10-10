namespace LevelMovement.Domain.ValueObjects.Vehicles;

public sealed record VehicleYear
{
    public int Value { get; }

    public VehicleYear(int value)
    {
        if (value is < 1000 or > 9999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, "Vehicle year must be between 1000 and 9999.");
        }

        Value = value;
    }
}
