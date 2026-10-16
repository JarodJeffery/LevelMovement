namespace LevelMovement.Application.Vehicles.DTOs;

public sealed record UpSertVehicleResponse(
    string RegistrationNumber,
    string Make,
    string Model,
    string Message,
    Guid ConcurrencyStamp);
