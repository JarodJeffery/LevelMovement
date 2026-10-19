using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.DTOs;
namespace LevelMovement.Application.Vehicles.Organisers;
public sealed class VehicleReadOrganiser(IVehiclesService service)
{
    public Task<VehiclePageResponse> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        => service.GetPageAsync(pageNumber, pageSize, cancellationToken);
    public Task<VehicleDetailsResponse> GetDetailsAsync(string registrationNumber, CancellationToken cancellationToken)
        => service.GetDetailsAsync(registrationNumber, cancellationToken);
}
