using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.Results;
using LevelMovement.Domain.Vehicles;

namespace LevelMovement.Infrastructure.Services;

public sealed class VehiclesService(IVehicleRepository repository) : IVehiclesService
{
    public async Task<IReadOnlyList<VehicleSummaryResponse>> GetAllAsync(CancellationToken cancellationToken)
        => (await repository.GetAllAsync(cancellationToken)).Select(vehicle =>
            new VehicleSummaryResponse(vehicle.RegistrationNumber.Value,
                vehicle.Make, vehicle.Model, vehicle.Year.Value, vehicle.DailyRate.Amount, vehicle.CreatedDate)).ToArray();

    public async Task<VehicleDetailsResponse> GetDetailsAsync(string registrationNumber, CancellationToken cancellationToken)
    {
        var vehicle = await repository.GetByRegistrationNumberAsync(new RegistrationNumber(registrationNumber), cancellationToken)
            ?? throw new KeyNotFoundException("Vehicle was not found.");
        return new(vehicle.RegistrationNumber.Value, vehicle.Make, vehicle.Model, vehicle.Year.Value,
            vehicle.DailyRate.Amount, vehicle.CreatedDate, vehicle.ConcurrencyStamp);
    }

    public Task DeleteAsync(string registrationNumber, Guid stamp, CancellationToken cancellationToken)
        => repository.DeleteAsync(new RegistrationNumber(registrationNumber), stamp, cancellationToken);

    public async Task<UpsertVehicleResult> UpsertVehicleAsync(
        Vehicle vehicle, CancellationToken cancellationToken)
    {
        var isCreated = vehicle.ConcurrencyStamp == Guid.Empty;
        var trackedVehicle = await repository.UpsertAsync(vehicle, cancellationToken);
        return new UpsertVehicleResult(trackedVehicle, isCreated);
    }
}
