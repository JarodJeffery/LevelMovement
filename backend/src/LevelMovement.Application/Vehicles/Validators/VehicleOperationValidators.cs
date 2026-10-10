using FluentValidation;
using LevelMovement.Application.Bookings.Queries;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.Queries;
using LevelMovement.Domain.ValueObjects.Vehicles;
namespace LevelMovement.Application.Vehicles.Validators;
public abstract class RegistrationValidator<T> : AbstractValidator<T>
{
    protected RegistrationValidator(System.Linq.Expressions.Expression<Func<T, string>> selector)
    {
        RuleFor(selector).NotEmpty().Must(value => value is null || value.Trim().ToUpperInvariant().Length <= RegistrationNumber.MaxLength)
            .WithMessage("Registration number must not exceed 20 characters.");
    }
}
public sealed class GetVehicleDetailsValidator : RegistrationValidator<GetVehicleDetailsQuery>
{
    public GetVehicleDetailsValidator() : base(request => request.RegistrationNumber) { }
}
public sealed class ValidateBookingValidator : RegistrationValidator<ValidateBookingQuery>
{
    public ValidateBookingValidator() : base(request => request.RegistrationNumber) { }
}
public sealed class DeleteVehicleValidator : RegistrationValidator<DeleteVehicleCommand>
{
    public DeleteVehicleValidator() : base(request => request.RegistrationNumber)
    {
        RuleFor(request => request.ConcurrencyStamp).NotEmpty();
    }
}
