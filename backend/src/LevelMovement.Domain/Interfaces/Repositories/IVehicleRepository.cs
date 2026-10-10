using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;

public interface IVehicleRepository
{
    Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken);
    Task<Vehicle?> GetForBookingAsync(RegistrationNumber registrationNumber, CancellationToken cancellationToken);
    Task DeleteAsync(RegistrationNumber registrationNumber, Guid stamp, CancellationToken cancellationToken);
    Task<Vehicle> UpsertAsync(Vehicle vehicle, CancellationToken cancellationToken);
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Vehicle?> GetByRegistrationNumberAsync(
        RegistrationNumber registrationNumber, CancellationToken cancellationToken);
}
