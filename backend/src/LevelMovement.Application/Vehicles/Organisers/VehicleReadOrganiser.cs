using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.DTOs;
namespace LevelMovement.Application.Vehicles.Organisers;
public sealed class VehicleReadOrganiser(IVehiclesService service)
{
    public Task<IReadOnlyList<VehicleSummaryResponse>> GetAllAsync(CancellationToken cancellationToken)
        => service.GetAllAsync(cancellationToken);
    public Task<VehicleDetailsResponse> GetDetailsAsync(string registrationNumber, CancellationToken cancellationToken)
        => service.GetDetailsAsync(registrationNumber, cancellationToken);
}
