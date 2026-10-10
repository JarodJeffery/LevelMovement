using LevelMovement.Application.Vehicles.Organisers;
using LevelMovement.Application.Vehicles.Results;
using MediatR;

namespace LevelMovement.Application.Vehicles.Commands;

public sealed class UpsertVehicleCommandHandler(UpsertVehicleOrganiser organiser)
    : IRequestHandler<UpsertVehicleCommand, UpsertVehicleResult>
{
    public Task<UpsertVehicleResult> Handle(UpsertVehicleCommand request, CancellationToken cancellationToken)
        => organiser.ExecuteAsync(request, cancellationToken);
}
