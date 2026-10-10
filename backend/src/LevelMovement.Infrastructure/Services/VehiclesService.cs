using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Domain.Vehicles;

namespace LevelMovement.Infrastructure.Services;

public sealed class VehiclesService : IVehiclesService
{
    private readonly IVehicleRepository _repository;

    public VehiclesService(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public Task UpsertVehicleAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken)
    {
        return _repository.UpsertAsync(vehicle, cancellationToken);
    }
}