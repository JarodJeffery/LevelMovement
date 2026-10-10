using LevelMovement.Domain.Vehicles;

namespace LevelMovement.Application.Interfaces.Services
{
    public interface IVehiclesService
    {
        Task UpsertVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);
    }
}
