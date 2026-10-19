using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Application.Vehicles.Organisers;
using MediatR;
namespace LevelMovement.Application.Vehicles.Queries;
public sealed record GetVehiclesQuery(int PageNumber = 1, int PageSize = 10) : IRequest<VehiclePageResponse>;
public sealed record GetVehicleDetailsQuery(string RegistrationNumber) : IRequest<VehicleDetailsResponse>;
public sealed class GetVehiclesHandler(VehicleReadOrganiser organiser) : IRequestHandler<GetVehiclesQuery, VehiclePageResponse>
{
    public Task<VehiclePageResponse> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
        => organiser.GetPageAsync(request.PageNumber, request.PageSize, cancellationToken);
}
public sealed class GetVehicleDetailsHandler(VehicleReadOrganiser organiser) : IRequestHandler<GetVehicleDetailsQuery, VehicleDetailsResponse>
{
    public Task<VehicleDetailsResponse> Handle(GetVehicleDetailsQuery request, CancellationToken cancellationToken)
        => organiser.GetDetailsAsync(request.RegistrationNumber, cancellationToken);
}
