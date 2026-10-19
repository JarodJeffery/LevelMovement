using FluentValidation;
using LevelMovement.Application.Vehicles.Queries;

namespace LevelMovement.Application.Vehicles.Validators;

public sealed class GetVehiclesValidator : AbstractValidator<GetVehiclesQuery>
{
    public GetVehiclesValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
