using LevelMovement.Application.Bookings.DTOs;
namespace LevelMovement.Application.Interfaces.Services;
public interface IBookingEligibilityService
{
    Task<BookingEligibilityResponse> ValidateAsync(string registrationNumber, CancellationToken cancellationToken);
}
