using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Domain.Vehicles;

namespace LevelMovement.Application.Vehicles.Results;

public sealed class UpsertVehicleResult(Vehicle vehicle, bool isCreated)
{
    public bool IsCreated { get; } = isCreated;

    public UpSertVehicleResponse ToResponse()
    {
        var action = IsCreated ? "Registered" : "Updated";
        return new UpSertVehicleResponse(
            vehicle.RegistrationNumber.Value,
            vehicle.Make,
            vehicle.Model,
            $"{action} vehicle {vehicle.RegistrationNumber.Value}, make {vehicle.Make}, model {vehicle.Model}.",
            vehicle.ConcurrencyStamp);
    }
}
