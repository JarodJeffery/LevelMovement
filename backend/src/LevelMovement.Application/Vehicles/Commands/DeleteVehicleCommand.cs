using LevelMovement.Application.Interfaces.Persistence;
using LevelMovement.Application.Vehicles.Organisers;
using MediatR;
namespace LevelMovement.Application.Vehicles.Commands;
public sealed record DeleteVehicleCommand(string RegistrationNumber, Guid ConcurrencyStamp) : ICommand<Unit>;
public sealed class DeleteVehicleCommandHandler(DeleteVehicleOrganiser organiser) : IRequestHandler<DeleteVehicleCommand, Unit>
{
    public async Task<Unit> Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
    {
        await organiser.ExecuteAsync(request, cancellationToken);
        return Unit.Value;
    }
}
