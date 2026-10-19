using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;

public interface IVehicleRepository
{
    Task<List<Vehicle>> GetAllAsync(int page, int size, CancellationToken cancellationToken);
    Task<int> CountActiveAsync(CancellationToken cancellationToken);
    Task<Vehicle?> GetForBookingAsync(RegistrationNumber registrationNumber, CancellationToken cancellationToken);
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);
    void Update(Vehicle vehicle, Guid expectedStamp);
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Vehicle?> GetByRegistrationNumberAsync(RegistrationNumber registrationNumber, CancellationToken cancellationToken);
}
