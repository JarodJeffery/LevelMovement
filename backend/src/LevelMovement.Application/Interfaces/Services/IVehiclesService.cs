using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Application.Vehicles.Results;
using LevelMovement.Domain.ValueObjects.Vehicles;

namespace LevelMovement.Application.Interfaces.Services;

public interface IVehiclesService
{
    Task<VehiclePageResponse> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<VehicleDetailsResponse> GetDetailsAsync(string registrationNumber, CancellationToken cancellationToken);
    Task DeleteAsync(string registrationNumber, Guid stamp, CancellationToken cancellationToken);
    Task<UpsertVehicleResult> RegisterAsync(RegistrationNumber registrationNumber, string make, string model,
        VehicleYear year, DailyRate dailyRate, CancellationToken cancellationToken);
    Task<UpsertVehicleResult> UpdateAsync(RegistrationNumber registrationNumber, string make, string model,
        VehicleYear year, DailyRate dailyRate, Guid expectedStamp, CancellationToken cancellationToken);
}
