using LevelMovement.Domain.Vehicles;

public interface IVehicleRepository
{
    Task UpsertAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken);

    Task<Vehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
}