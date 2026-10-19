using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.Results;
using LevelMovement.Domain.Vehicles;

namespace LevelMovement.Application.Services;

public sealed class VehiclesService(IVehicleRepository repository) : IVehiclesService
{
    public async Task<VehiclePageResponse> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await repository.CountActiveAsync(cancellationToken);
        var totalPages = (int)(((long)totalCount + pageSize - 1) / pageSize);
        var vehicles = pageNumber > totalPages
            ? new List<Vehicle>()
            : await repository.GetAllAsync(pageNumber, pageSize, cancellationToken);
        var items = vehicles.Select(vehicle => new VehicleSummaryResponse(
            vehicle.RegistrationNumber.Value, vehicle.Make, vehicle.Model, vehicle.Year.Value,
            vehicle.DailyRate.Amount, vehicle.CreatedDate)).ToArray();
        return new VehiclePageResponse(items, pageNumber, totalPages, items.Length);
    }

    public async Task<VehicleDetailsResponse> GetDetailsAsync(string registrationNumber, CancellationToken cancellationToken)
    {
        var vehicle = await FindAsync(new RegistrationNumber(registrationNumber), cancellationToken);
        return new(vehicle.RegistrationNumber.Value, vehicle.Make, vehicle.Model, vehicle.Year.Value,
            vehicle.DailyRate.Amount, vehicle.CreatedDate, vehicle.ConcurrencyStamp);
    }

    public async Task DeleteAsync(string registrationNumber, Guid stamp, CancellationToken cancellationToken)
    {
        RequireStamp(stamp);
        var vehicle = await FindAsync(new RegistrationNumber(registrationNumber), cancellationToken);
        vehicle.Delete();
        repository.Update(vehicle, stamp);
    }

    public async Task<UpsertVehicleResult> RegisterAsync(RegistrationNumber registrationNumber, string make, string model,
        VehicleYear year, DailyRate dailyRate, CancellationToken cancellationToken)
    {
        var vehicle = Vehicle.Register(registrationNumber, make, model, year, dailyRate);
        await repository.AddAsync(vehicle, cancellationToken);
        return new UpsertVehicleResult(vehicle, isCreated: true);
    }

    public async Task<UpsertVehicleResult> UpdateAsync(RegistrationNumber registrationNumber, string make, string model,
        VehicleYear year, DailyRate dailyRate, Guid expectedStamp, CancellationToken cancellationToken)
    {
        RequireStamp(expectedStamp);
        var vehicle = await FindAsync(registrationNumber, cancellationToken);
        vehicle.UpdateDetails(make, model, year, dailyRate);
        repository.Update(vehicle, expectedStamp);
        return new UpsertVehicleResult(vehicle, isCreated: false);
    }

    private async Task<Vehicle> FindAsync(RegistrationNumber registrationNumber, CancellationToken cancellationToken)
        => await repository.GetByRegistrationNumberAsync(registrationNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Vehicle was not found.");

    private static void RequireStamp(Guid stamp)
    {
        if (stamp == Guid.Empty) throw new ArgumentException("A concurrency stamp is required.", nameof(stamp));
    }
}
