using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;

namespace LevelMovement.Application.Vehicles.Organisers;

public class CreateVehicleOrganiser
{
    private readonly IVehiclesService _service;

    public CreateVehicleOrganiser(IVehiclesService service)
    {
        _service = service;
    }

    public async Task<Guid> ExecuteAsync(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            RegistrationNumber = new RegistrationNumber(command.RegistrationNumber),
            Make = command.Make,
            Model = command.Model,
            Year = new VehicleYear(command.Year),
            DailyRate = new DailyRate(command.DailyRate),
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };
        await _service.UpsertVehicleAsync(vehicle, cancellationToken);
        return vehicle.Id;
    }
}