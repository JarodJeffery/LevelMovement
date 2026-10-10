using LevelMovement.Domain.Vehicles;
using LevelMovement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

public class VehicleRepository : IVehicleRepository
{
    private readonly LevelMovementDbContext _context;

    public VehicleRepository(LevelMovementDbContext context)
    {
        _context = context;
    }
    
    public async Task UpsertAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        var existing = await GetByIdAsync(vehicle.Id, cancellationToken);
        if (existing is null)
        {
            _context.Vehicles.Add(vehicle);
        }
        else
        {
            //.copy values from incoming vehicle to tracked entity
            _context.Entry(existing).CurrentValues.SetValues(vehicle);
        }

    }

    public Task<Vehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _context.Vehicles.SingleOrDefaultAsync(
            vehicle => vehicle.Id == id,
            cancellationToken);
    }
}
