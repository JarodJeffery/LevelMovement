using LevelMovement.Application.Vehicles.Organisers;
using MediatR;

namespace LevelMovement.Application.Vehicles.Commands;

public sealed class CreateVehicleCommandHandler(CreateVehicleOrganiser organiser)
    : IRequestHandler<CreateVehicleCommand, Guid>
{
    public Task<Guid> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
        => organiser.ExecuteAsync(request, cancellationToken);
}
