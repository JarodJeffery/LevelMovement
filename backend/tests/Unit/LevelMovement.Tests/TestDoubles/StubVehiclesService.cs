using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Application.Vehicles.Results;
using LevelMovement.Domain.ValueObjects.Vehicles;

namespace LevelMovement.Tests.TestDoubles;

internal sealed class StubVehiclesService : IVehiclesService
{
    public Func<int, int, CancellationToken, Task<VehiclePageResponse>> GetPage { get; set; }
        = (_, _, _) => throw new NotSupportedException();
    public Func<RegistrationNumber, string, string, VehicleYear, DailyRate, CancellationToken,
        Task<UpsertVehicleResult>> Register { get; set; }
        = (_, _, _, _, _, _) => throw new NotSupportedException();
    public Func<RegistrationNumber, string, string, VehicleYear, DailyRate, Guid, CancellationToken,
        Task<UpsertVehicleResult>> Update { get; set; }
        = (_, _, _, _, _, _, _) => throw new NotSupportedException();

    public int GetPageCalls { get; private set; }
    public int RegisterCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Task<VehiclePageResponse> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        GetPageCalls++;
        return GetPage(pageNumber, pageSize, cancellationToken);
    }

    public Task<UpsertVehicleResult> RegisterAsync(RegistrationNumber registrationNumber, string make,
        string model, VehicleYear year, DailyRate dailyRate, CancellationToken cancellationToken)
    {
        RegisterCalls++;
        return Register(registrationNumber, make, model, year, dailyRate, cancellationToken);
    }

    public Task<UpsertVehicleResult> UpdateAsync(RegistrationNumber registrationNumber, string make,
        string model, VehicleYear year, DailyRate dailyRate, Guid expectedStamp, CancellationToken cancellationToken)
    {
        UpdateCalls++;
        return Update(registrationNumber, make, model, year, dailyRate, expectedStamp, cancellationToken);
    }

    public Task<VehicleDetailsResponse> GetDetailsAsync(string registrationNumber, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task DeleteAsync(string registrationNumber, Guid stamp, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
