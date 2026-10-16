using LevelMovement.Application.Bookings.DTOs;
using LevelMovement.Application.Interfaces.Services;
namespace LevelMovement.Application.Bookings.Organisers;
public sealed class BookingEligibilityOrganiser(IBookingEligibilityService service)
{
    public Task<BookingEligibilityResponse> ExecuteAsync(string registrationNumber, CancellationToken cancellationToken)
        => service.ValidateAsync(registrationNumber, cancellationToken);
}
