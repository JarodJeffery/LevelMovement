using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LevelMovement.Api.Middleware;
using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Application.Vehicles.Results;
using LevelMovement.Domain.Vehicles;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
namespace LevelMovement.Tests;
public sealed class VehicleCrudTests
{
    [Fact]
    public async Task ReadsReturnDetailsStampButListOmitsStampAndIdWithoutSaving()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var created = await Create(client);
        var list = await client.GetFromJsonAsync<JsonElement>("/api/vehicles");
        Assert.Equal(1, list.GetArrayLength());
        Assert.False(list[0].TryGetProperty("concurrencyStamp", out _));
        Assert.False(list[0].TryGetProperty("id", out _));
        var details = await client.GetFromJsonAsync<JsonElement>("/api/vehicles/ab123%20gp");
        Assert.Equal(created.ConcurrencyStamp, details.GetProperty("concurrencyStamp").GetGuid());
        Assert.False(details.TryGetProperty("id", out _));
        Assert.Equal(1, factory.SaveCalls);
    }

    [Fact]
    public async Task DeletePreservesRecordAndDateButHidesItAndRejectsBookingWithoutFurtherSaves()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var created = await Create(client);
        var before = await client.GetFromJsonAsync<VehicleDetailsResponse>("/api/vehicles/AB123%20GP");
        var booking = await client.PostAsJsonAsync("/api/bookings/validate", new { registrationNumber = "AB123 GP" });
        Assert.Equal(HttpStatusCode.OK, booking.StatusCode);
        Assert.True((await booking.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eligible").GetBoolean());
        Assert.Equal(1, factory.SaveCalls);
        var deletion = await Delete(client, created.ConcurrencyStamp);
        Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/vehicles/AB123%20GP")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<VehicleSummaryResponse[]>("/api/vehicles"))!);
        var rejected = await client.PostAsJsonAsync("/api/bookings/validate", new { registrationNumber = "AB123 GP" });
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        var error = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Vehicle has been deleted.", error.GetProperty("detail").GetString());
        Assert.False(error.TryGetProperty("traceId", out _));
        Assert.Equal(HttpStatusCode.NotFound, (await Delete(client, created.ConcurrencyStamp)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var entity = await scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>().Vehicles.IgnoreQueryFilters().SingleAsync();
        Assert.True(entity.IsDeleted);
        Assert.Equal(before!.CreatedDate, entity.CreatedDate);
        Assert.NotEqual(created.ConcurrencyStamp, entity.ConcurrencyStamp);
        Assert.Equal(2, factory.SaveCalls);
    }

    [Fact]
    public async Task StaleDeleteDoesNotDeleteVehicle()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var created = await Create(client);
        var updated = await client.PostAsJsonAsync("/api/vehicles", new UpsertVehicleCommand("AB123 GP", "Toyota", "Yaris", 2024, 11m, created.ConcurrencyStamp));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Delete(client, created.ConcurrencyStamp)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/vehicles/AB123%20GP")).StatusCode);
        Assert.Equal(2, factory.SaveCalls);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"concurrencyStamp\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"concurrencyStamp\":invalid}")]
    public async Task InvalidDeleteDoesNotExposeTraceAndDoesNotSave(string body)
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/vehicles/AB123%20GP") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("traceId", out _));
        Assert.Equal(0, factory.SaveCalls);
    }

    [Fact]
    public async Task MissingDetailDeleteAndBookingReturn404WithoutSaves()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/vehicles/MISSING")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Delete(client, Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/bookings/validate", new { registrationNumber = "MISSING" })).StatusCode);
        Assert.Equal(0, factory.SaveCalls);
    }

    [Fact]
    public async Task UnexpectedFailureLogsTraceRequestAndFunctionWithoutExposingDiagnostics()
    {
        var logger = new CaptureLogger();
        using var factory = new VehicleApiTests.VehicleApiFactory(services =>
        {
            services.RemoveAll<IVehiclesService>();
            services.AddScoped<IVehiclesService, FailingService>();
            services.AddSingleton<ILogger<ApiExceptionHandler>>(logger);
        });
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/api/vehicles");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(raw);
        Assert.DoesNotContain("private-database-secret", raw);
        Assert.Equal("An unexpected error occurred.", problem.GetProperty("title").GetString());
        var log = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.False(problem.TryGetProperty("traceId", out _));
        Assert.False(string.IsNullOrWhiteSpace(log.Values["TraceId"]?.ToString()));
        Assert.Equal("GetVehiclesQuery", log.Values["CqrsRequest"]);
        Assert.Contains("FailingService.GetAllAsync", log.Values["FailingFunction"]!.ToString());
        Assert.NotNull(log.Exception);
    }

    [Fact]
    public async Task ExpectedFailureLogsWarningWithTraceOnlyInLogs()
    {
        var logger = new CaptureLogger();
        using var factory = new VehicleApiTests.VehicleApiFactory(services => services.AddSingleton<ILogger<ApiExceptionHandler>>(logger));
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/api/vehicles/MISSING");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var log = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, log.Level);
        Assert.False(body.TryGetProperty("traceId", out _));
        Assert.False(string.IsNullOrWhiteSpace(log.Values["TraceId"]?.ToString()));
        Assert.Contains("VehiclesService.FindAsync", log.Values["FailingFunction"]!.ToString());
    }

    [Fact]
    public async Task SwaggerDocumentsReadDeleteBookingAndTheirExamples()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        foreach (var (path, method) in new[] { ("/api/vehicles", "get"), ("/api/vehicles/{registrationNumber}", "get"), ("/api/vehicles/{registrationNumber}", "delete"), ("/api/bookings/validate", "post") })
        {
            var operation = document.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            foreach (var response in operation.GetProperty("responses").EnumerateObject())
                if (response.Value.TryGetProperty("content", out var content))
                    Assert.All(content.EnumerateObject(), media => Assert.True(media.Value.TryGetProperty("example", out _)));
        }
    }

    private static async Task<UpSertVehicleResponse> Create(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/vehicles", new UpsertVehicleCommand("AB123 GP", "Toyota", "Corolla", 2024, 10m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UpSertVehicleResponse>())!;
    }
    private static Task<HttpResponseMessage> Delete(HttpClient client, Guid stamp)
        => client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/vehicles/AB123%20GP") { Content = JsonContent.Create(new DeleteVehicleRequest(stamp)) });
    private sealed class FailingService : IVehiclesService
    {
        public Task<IReadOnlyList<VehicleSummaryResponse>> GetAllAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("private-database-secret");
        public Task<VehicleDetailsResponse> GetDetailsAsync(string registrationNumber, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task DeleteAsync(string registrationNumber, Guid stamp, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<UpsertVehicleResult> RegisterAsync(RegistrationNumber registrationNumber, string make, string model,
            VehicleYear year, DailyRate dailyRate, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<UpsertVehicleResult> UpdateAsync(RegistrationNumber registrationNumber, string make, string model,
            VehicleYear year, DailyRate dailyRate, Guid expectedStamp, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
    private sealed class CaptureLogger : ILogger<ApiExceptionHandler>
    {
        public List<(LogLevel Level, Dictionary<string, object?> Values, Exception? Exception)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(pair => pair.Key, pair => pair.Value), exception));
    }
}
