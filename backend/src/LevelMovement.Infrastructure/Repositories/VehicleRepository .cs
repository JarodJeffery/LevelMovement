using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;
using LevelMovement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class VehicleRepository(LevelMovementDbContext context) : IVehicleRepository
{
    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        await context.Vehicles.AddAsync(vehicle, cancellationToken);
    }

    public void Update(Vehicle vehicle, Guid expectedStamp)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        if (expectedStamp == Guid.Empty) throw new ArgumentException("A concurrency stamp is required.", nameof(expectedStamp));
        var entry = context.Entry(vehicle);
        if (entry.State is EntityState.Detached or EntityState.Added)
            throw new InvalidOperationException("Load the persisted vehicle before staging an update.");
        entry.Property(item => item.ConcurrencyStamp).OriginalValue = expectedStamp;
        // Issue a write even for unchanged details so stale no-op edits still fail.
        entry.Property(item => item.Make).IsModified = true;
    }

    public Task<int> CountActiveAsync(CancellationToken cancellationToken)
        => context.Vehicles.CountAsync(cancellationToken);

    public Task<List<Vehicle>> GetAllAsync(int page, int size, CancellationToken cancellationToken)
    {
        var offset = ((long)page - 1) * size;
        if (offset > int.MaxValue) return Task.FromResult(new List<Vehicle>());

        return context.Vehicles.AsNoTracking()
            .OrderBy(vehicle => vehicle.RegistrationNumber)
            .Skip((int)offset)
            .Take(size)
            .ToListAsync(cancellationToken);
    }

    public Task<Vehicle?> GetForBookingAsync(RegistrationNumber registrationNumber, CancellationToken cancellationToken)
        => context.Vehicles.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(
            vehicle => vehicle.RegistrationNumber == registrationNumber, cancellationToken);

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.Vehicles.SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public Task<Vehicle?> GetByRegistrationNumberAsync(RegistrationNumber registrationNumber, CancellationToken cancellationToken)
        => context.Vehicles.SingleOrDefaultAsync(
            vehicle => vehicle.RegistrationNumber == registrationNumber, cancellationToken);
}
