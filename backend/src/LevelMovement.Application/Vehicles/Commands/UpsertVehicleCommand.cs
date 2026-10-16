using LevelMovement.Application.Interfaces.Persistence;
using LevelMovement.Application.Vehicles.Results;

namespace LevelMovement.Application.Vehicles.Commands;

public sealed record UpsertVehicleCommand(
    string RegistrationNumber,
    string Make,
    string Model,
    int Year,
    decimal DailyRate,
    Guid? ConcurrencyStamp = null) : ICommand<UpsertVehicleResult>;
