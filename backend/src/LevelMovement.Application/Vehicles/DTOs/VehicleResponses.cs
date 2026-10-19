namespace LevelMovement.Application.Vehicles.DTOs;
public sealed record VehicleSummaryResponse(string RegistrationNumber, string Make, string Model,
    int Year, decimal DailyRate, DateTime CreatedDate);
public sealed record VehiclePageResponse(IReadOnlyList<VehicleSummaryResponse> Items,
    int PageNumber, int TotalPages, int Count);
public sealed record VehicleDetailsResponse(string RegistrationNumber, string Make, string Model,
    int Year, decimal DailyRate, DateTime CreatedDate, Guid ConcurrencyStamp);
public sealed record DeleteVehicleRequest(Guid ConcurrencyStamp);
