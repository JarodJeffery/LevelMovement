namespace LevelMovement.Application.Bookings.DTOs;
public sealed record BookingEligibilityResponse(string RegistrationNumber, bool Eligible, string Message);
