using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.Results;
using LevelMovement.Domain.ValueObjects.Vehicles;

namespace LevelMovement.Application.Vehicles.Organisers;

public sealed class UpsertVehicleOrganiser(IVehiclesService service)
{
    public Task<UpsertVehicleResult> ExecuteAsync(UpsertVehicleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var registration = new RegistrationNumber(command.RegistrationNumber);
        var year = new VehicleYear(command.Year);
        var rate = new DailyRate(command.DailyRate);
        // Adapt the existing HTTP upsert contract into explicit application operations.
        return command.ConcurrencyStamp is { } stamp
            ? service.UpdateAsync(registration, command.Make, command.Model, year, rate, stamp, cancellationToken)
            : service.RegisterAsync(registration, command.Make, command.Model, year, rate, cancellationToken);
    }
}
