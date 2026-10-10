using LevelMovement.Application.Bookings.DTOs;
using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Domain.Managers;
using LevelMovement.Domain.ValueObjects.Vehicles;
namespace LevelMovement.Infrastructure.Services;
public sealed class BookingEligibilityService(IVehicleRepository repository, VehicleBookingManager manager) : IBookingEligibilityService
{
    public async Task<BookingEligibilityResponse> ValidateAsync(string registrationNumber, CancellationToken cancellationToken)
    {
        var vehicle = await repository.GetForBookingAsync(new RegistrationNumber(registrationNumber), cancellationToken)
            ?? throw new KeyNotFoundException("Vehicle was not found.");
        manager.EnsureEligible(vehicle);
        return new(vehicle.RegistrationNumber.Value, true, $"Vehicle {vehicle.RegistrationNumber.Value} is eligible for booking. No booking has been created.");
    }
}
