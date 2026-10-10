using MediatR;

namespace LevelMovement.Application.Vehicles.Commands
{
    public record CreateVehicleCommand(
    string RegistrationNumber,
    string Make,
    string Model,
    int Year,
    decimal DailyRate) : LevelMovement.Application.Interfaces.Persistence.ICommand<Guid>;
}
