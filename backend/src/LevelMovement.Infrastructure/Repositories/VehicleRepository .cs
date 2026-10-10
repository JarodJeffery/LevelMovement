using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;
using LevelMovement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class VehicleRepository(LevelMovementDbContext context) : IVehicleRepository
{
    public async Task<Vehicle> UpsertAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        if (vehicle.ConcurrencyStamp == Guid.Empty)
        {
            context.Vehicles.Add(vehicle);
            return vehicle;
        }

        var existing = await GetByRegistrationNumberAsync(vehicle.RegistrationNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Vehicle was not found.");

        context.Entry(existing).Property(item => item.ConcurrencyStamp).OriginalValue =
            vehicle.ConcurrencyStamp;

        existing.Make = vehicle.Make;
        existing.Model = vehicle.Model;
        existing.Year = vehicle.Year;
        existing.DailyRate = vehicle.DailyRate;

        context.Entry(existing).Property(item => item.Make).IsModified = true;
        return existing;
    }

    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken)
        => await context.Vehicles.AsNoTracking().OrderBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync(cancellationToken);

    public Task<Vehicle?> GetForBookingAsync(RegistrationNumber registrationNumber, CancellationToken cancellationToken)
        => context.Vehicles.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(
            vehicle => vehicle.RegistrationNumber == registrationNumber, cancellationToken);

    public async Task DeleteAsync(RegistrationNumber registrationNumber, Guid stamp, CancellationToken cancellationToken)
    {
        var vehicle = await GetByRegistrationNumberAsync(registrationNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Vehicle was not found.");
        context.Entry(vehicle).Property(item => item.ConcurrencyStamp).OriginalValue = stamp;
        vehicle.IsDeleted = true;
    }

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.Vehicles.SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public Task<Vehicle?> GetByRegistrationNumberAsync(
        RegistrationNumber registrationNumber, CancellationToken cancellationToken)
        => context.Vehicles.SingleOrDefaultAsync(
            vehicle => vehicle.RegistrationNumber == registrationNumber, cancellationToken);
}
