using FluentValidation;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Domain.ValueObjects.Vehicles;

namespace LevelMovement.Application.Vehicles.Validators;

public sealed class UpsertVehicleCommandValidator : AbstractValidator<UpsertVehicleCommand>
{
    public UpsertVehicleCommandValidator()
    {
        RuleFor(command => command.ConcurrencyStamp)
            .Must(stamp => !stamp.HasValue || stamp.Value != Guid.Empty)
            .WithMessage("For inserts, omit the stamp. For updates, provide a non-empty stamp.");
        RuleFor(command => command.RegistrationNumber)
            .NotEmpty()
            .Must(value => value is null ||
                value.Trim().ToUpperInvariant().Length <= RegistrationNumber.MaxLength)
            .WithMessage("Registration number must not exceed 20 characters.");
        RuleFor(command => command.Make).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Model).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Year).InclusiveBetween(1000, 9999);
        RuleFor(command => command.DailyRate)
            .Must(value => decimal.Round(value, 2, MidpointRounding.ToEven)
                is >= -9999999999999999.99m and <= 9999999999999999.99m)
            .WithMessage("Daily rate is invalid.");
    }
}
