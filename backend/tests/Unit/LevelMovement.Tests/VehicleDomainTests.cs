using LevelMovement.Domain.Exceptions;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;

namespace LevelMovement.Tests;

public sealed class VehicleDomainTests
{
    private static Vehicle Register() => Vehicle.Register(new("KW 00 KB GP"), "Toyota", "Corolla", new(2024), new(100m));

    [Fact]
    public void RegistrationCreatesAnActiveIdentifiedVehicle()
    {
        var before = DateTime.UtcNow;
        var vehicle = Register();
        Assert.NotEqual(Guid.Empty, vehicle.Id);
        Assert.Equal("KW00KBGP", vehicle.RegistrationNumber.Value);
        Assert.False(vehicle.IsDeleted);
        Assert.Equal(DateTimeKind.Utc, vehicle.CreatedDate.Kind);
        Assert.InRange(vehicle.CreatedDate, before, DateTime.UtcNow);
        vehicle.EnsureEligibleForBooking();
    }

    [Theory]
    [InlineData("", "Corolla")]
    [InlineData("Toyota", " ")]
    [InlineData(null, "Corolla")]
    [InlineData("Toyota", null)]
    public void InvalidDetailsCannotCreateOrPartiallyChangeAVehicle(string? make, string? model)
    {
        Assert.ThrowsAny<ArgumentException>(() => Vehicle.Register(new("TEST"), make!, model!, new(2025), new(200m)));
        var vehicle = Register();
        Assert.ThrowsAny<ArgumentException>(() => vehicle.UpdateDetails(make!, model!, new(2025), new(200m)));
        Assert.Equal("Toyota", vehicle.Make);
        Assert.Equal("Corolla", vehicle.Model);
        Assert.Equal(2024, vehicle.Year.Value);
        Assert.Equal(100m, vehicle.DailyRate.Amount);
    }

    [Fact]
    public void TypedValuesAndTextLengthsAreEnforcedWithoutApplicationValidation()
    {
        Assert.Throws<ArgumentNullException>(() => Vehicle.Register(null!, "Toyota", "Corolla", new(2024), new(100m)));
        Assert.Throws<ArgumentException>(() => Vehicle.Register(new("TEST"), new string('A', 101), "Corolla", new(2024), new(100m)));
        var vehicle = Register();
        Assert.Throws<ArgumentException>(() => vehicle.UpdateDetails("Ford", new string('A', 101), new(2025), new(200m)));
        Assert.Throws<ArgumentNullException>(() => vehicle.UpdateDetails("Ford", "Fiesta", null!, new(200m)));
        Assert.Throws<ArgumentNullException>(() => vehicle.UpdateDetails("Ford", "Fiesta", new(2025), null!));
        Assert.Equal("Toyota", vehicle.Make);
        Assert.Equal("Corolla", vehicle.Model);
    }

    [Fact]
    public void ValidUpdatePreservesIdentityRegistrationAndCreationDate()
    {
        var vehicle = Register();
        var identity = (vehicle.Id, vehicle.RegistrationNumber, vehicle.CreatedDate);
        vehicle.UpdateDetails("Ford", "Fiesta", new(2025), new(200m));
        Assert.Equal(identity, (vehicle.Id, vehicle.RegistrationNumber, vehicle.CreatedDate));
        Assert.Equal("Ford", vehicle.Make);
        Assert.Equal("Fiesta", vehicle.Model);
        Assert.Equal(2025, vehicle.Year.Value);
        Assert.Equal(200m, vehicle.DailyRate.Amount);
    }

    [Fact]
    public void DeletedVehicleCannotBeChangedOrBooked()
    {
        var vehicle = Register();
        var identity = (vehicle.Id, vehicle.RegistrationNumber, vehicle.CreatedDate);
        vehicle.Delete();
        Assert.True(vehicle.IsDeleted);
        Assert.Equal(identity, (vehicle.Id, vehicle.RegistrationNumber, vehicle.CreatedDate));
        Assert.Throws<InvalidOperationException>(() => vehicle.UpdateDetails("Ford", "Fiesta", new(2025), new(200m)));
        Assert.Throws<InvalidOperationException>(() => vehicle.Delete());
        Assert.Throws<VehicleUnavailableException>(() => vehicle.EnsureEligibleForBooking());
        Assert.Equal("Toyota", vehicle.Make);
    }
}
