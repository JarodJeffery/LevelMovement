using LevelMovement.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LevelMovement.Infrastructure.Persistence.Interceptors;

public sealed class VehicleConcurrencyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        AssignStamps(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AssignStamps(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void AssignStamps(DbContext? context)
    {
        if (context is null) return;
        foreach (var entry in context.ChangeTracker.Entries<Vehicle>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Property(vehicle => vehicle.ConcurrencyStamp).CurrentValue = Guid.NewGuid();
        }
    }
}
