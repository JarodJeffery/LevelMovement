using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Application.Vehicles.Organisers;
using MediatR;
namespace LevelMovement.Application.Vehicles.Queries;
public sealed record GetVehiclesQuery : IRequest<IReadOnlyList<VehicleSummaryResponse>>;
public sealed record GetVehicleDetailsQuery(string RegistrationNumber) : IRequest<VehicleDetailsResponse>;
public sealed class GetVehiclesHandler(VehicleReadOrganiser organiser) : IRequestHandler<GetVehiclesQuery, IReadOnlyList<VehicleSummaryResponse>>
{
    public Task<IReadOnlyList<VehicleSummaryResponse>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
        => organiser.GetAllAsync(cancellationToken);
}
public sealed class GetVehicleDetailsHandler(VehicleReadOrganiser organiser) : IRequestHandler<GetVehicleDetailsQuery, VehicleDetailsResponse>
{
    public Task<VehicleDetailsResponse> Handle(GetVehicleDetailsQuery request, CancellationToken cancellationToken)
        => organiser.GetDetailsAsync(request.RegistrationNumber, cancellationToken);
}
