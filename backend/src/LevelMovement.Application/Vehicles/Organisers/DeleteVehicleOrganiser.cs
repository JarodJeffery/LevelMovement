using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.Commands;
namespace LevelMovement.Application.Vehicles.Organisers;
public sealed class DeleteVehicleOrganiser(IVehiclesService service)
{
    public Task ExecuteAsync(DeleteVehicleCommand command, CancellationToken cancellationToken)
        => service.DeleteAsync(command.RegistrationNumber, command.ConcurrencyStamp, cancellationToken);
}
