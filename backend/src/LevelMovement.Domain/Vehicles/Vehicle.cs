using LevelMovement.Domain.Exceptions;
using LevelMovement.Domain.ValueObjects.Vehicles;

namespace LevelMovement.Domain.Vehicles;

public sealed class Vehicle
{
    public Guid Id { get; private set; }
    public RegistrationNumber RegistrationNumber { get; private set; } = null!;
    public string Make { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public VehicleYear Year { get; private set; } = null!;
    public DailyRate DailyRate { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public Guid ConcurrencyStamp { get; private set; }

    // EF materializes persisted state through this constructor and private setters.
    private Vehicle() { }

    public static Vehicle Register(RegistrationNumber registrationNumber, string make, string model,
        VehicleYear year, DailyRate dailyRate)
    {
        ArgumentNullException.ThrowIfNull(registrationNumber);
        ValidateDetails(make, model, year, dailyRate);
        return new Vehicle
        {
            Id = Guid.NewGuid(), RegistrationNumber = registrationNumber,
            Make = make, Model = model, Year = year, DailyRate = dailyRate,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string make, string model, VehicleYear year, DailyRate dailyRate)
    {
        EnsureActive();
        // Validate everything before mutating, so a rejected change leaves the entity intact.
        ValidateDetails(make, model, year, dailyRate);
        Make = make;
        Model = model;
        Year = year;
        DailyRate = dailyRate;
    }

    public void Delete()
    {
        EnsureActive();
        IsDeleted = true;
    }

    public void EnsureEligibleForBooking()
    {
        if (IsDeleted) throw new VehicleUnavailableException();
    }

    private void EnsureActive()
    {
        if (IsDeleted) throw new InvalidOperationException("Deleted vehicles cannot be changed.");
    }

    private static void ValidateDetails(string make, string model, VehicleYear year, DailyRate dailyRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(make);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        if (make.Length > 100) throw new ArgumentException("Make must not exceed 100 characters.", nameof(make));
        if (model.Length > 100) throw new ArgumentException("Model must not exceed 100 characters.", nameof(model));
        ArgumentNullException.ThrowIfNull(year);
        ArgumentNullException.ThrowIfNull(dailyRate);
    }
}
