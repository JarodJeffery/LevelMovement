using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.Results;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;

namespace LevelMovement.Application.Vehicles.Organisers;

public sealed class UpsertVehicleOrganiser(IVehiclesService service)
{
    public Task<UpsertVehicleResult> ExecuteAsync(
        UpsertVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle
        {
            Id = command.ConcurrencyStamp.HasValue ? Guid.Empty : Guid.NewGuid(),
            ConcurrencyStamp = command.ConcurrencyStamp ?? Guid.Empty,
            RegistrationNumber = new RegistrationNumber(command.RegistrationNumber),
            Make = command.Make,
            Model = command.Model,
            Year = new VehicleYear(command.Year),
            DailyRate = new DailyRate(command.DailyRate),
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };
        return service.UpsertVehicleAsync(vehicle, cancellationToken);
    }
}
