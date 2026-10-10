namespace LevelMovement.Domain.Vehicles;

using LevelMovement.Domain.ValueObjects.Vehicles;

public class Vehicle
{
    public Guid Id { get; set; }

    public required RegistrationNumber RegistrationNumber { get; set; }

    public required string Make { get; set; }

    public required string Model { get; set; }

    public required VehicleYear Year { get; set; }

    public required DailyRate DailyRate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
