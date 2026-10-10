using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Application.Vehicles.Results;
using LevelMovement.Domain.Vehicles;

namespace LevelMovement.Application.Interfaces.Services;

public interface IVehiclesService
{
    Task<IReadOnlyList<VehicleSummaryResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<VehicleDetailsResponse> GetDetailsAsync(string registrationNumber, CancellationToken cancellationToken);
    Task DeleteAsync(string registrationNumber, Guid stamp, CancellationToken cancellationToken);
    Task<UpsertVehicleResult> UpsertVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);
}
