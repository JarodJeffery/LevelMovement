using LevelMovement.Application.Bookings.DTOs;
using LevelMovement.Application.Bookings.Organisers;
using MediatR;
namespace LevelMovement.Application.Bookings.Queries;
public sealed record ValidateBookingQuery(string RegistrationNumber) : IRequest<BookingEligibilityResponse>;
public sealed class ValidateBookingHandler(BookingEligibilityOrganiser organiser) : IRequestHandler<ValidateBookingQuery, BookingEligibilityResponse>
{
    public Task<BookingEligibilityResponse> Handle(ValidateBookingQuery request, CancellationToken cancellationToken)
        => organiser.ExecuteAsync(request.RegistrationNumber, cancellationToken);
}
