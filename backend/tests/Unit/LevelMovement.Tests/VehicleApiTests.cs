using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Application.Interfaces.Persistence;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Infrastructure.Persistence;
using LevelMovement.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using LevelMovement.Api.Security;

namespace LevelMovement.Tests;

public sealed class VehicleApiTests
{
    [Fact]
    public async Task InsertReturnsNormalizedDetailsAndSavedStampWithoutId()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand(" ab123 gp ", "Toyota", "Corolla", 2024, 10.125m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseText = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UpSertVehicleResponse>(
            responseText, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(result);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>();
        var vehicle = await context.Vehicles.SingleAsync();
        Assert.Equal("AB123GP", result.RegistrationNumber);
        Assert.Equal(vehicle.Make, result.Make);
        Assert.Equal(vehicle.Model, result.Model);
        Assert.Equal("Registered vehicle AB123GP, make Toyota, model Corolla.", result.Message);
        Assert.Equal("AB123GP", vehicle.RegistrationNumber.Value);
        Assert.Equal(10.12m, vehicle.DailyRate.Amount);
        Assert.NotEqual(Guid.Empty, vehicle.ConcurrencyStamp);
        Assert.Equal(vehicle.ConcurrencyStamp, result.ConcurrencyStamp);
        using var json = JsonDocument.Parse(responseText);
        Assert.False(json.RootElement.TryGetProperty("id", out _));
        Assert.False(vehicle.IsDeleted);
        Assert.Equal(1, factory.SaveCalls);
    }

    [Fact]
    public async Task InvalidCommandReturnsFieldErrorsWithoutSaving()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand("", "", "Corolla", 999, 10m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("year", out _));
        Assert.True(body.GetProperty("errors").TryGetProperty("registrationNumber", out _));
        Assert.Equal(0, factory.SaveCalls);
        using var scope = factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>()
            .Vehicles.AnyAsync());
    }

    [Fact]
    public async Task SwaggerIncludesBothRequestsAndResponseExamples()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var operation = document.GetProperty("paths").GetProperty("/api/vehicles").GetProperty("post");
        var examples = operation.GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("examples");
        Assert.Equal(450m, examples.GetProperty("standard").GetProperty("value").GetProperty("dailyRate").GetDecimal());
        Assert.Equal(10.125m, examples.GetProperty("rounding").GetProperty("value").GetProperty("dailyRate").GetDecimal());
        Assert.True(examples.TryGetProperty("update", out _));
        var requestSchema = document.GetProperty("components").GetProperty("schemas")
            .GetProperty("UpsertVehicleCommand").GetProperty("properties");
        Assert.False(requestSchema.TryGetProperty("id", out _));
        foreach (var status in new[] { "200", "201", "400", "404", "409" })
        {
            var content = operation.GetProperty("responses").GetProperty(status).GetProperty("content");
            Assert.All(content.EnumerateObject(), media => Assert.True(media.Value.TryGetProperty("example", out _)));
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }

    [Fact]
    public async Task UpdateByRegistrationPreservesIdentityAndCreatedDateAndReturnsSavedStamp()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var created = await Insert(client);
        Guid originalId;
        DateTime originalDate;
        using (var scope = factory.Services.CreateScope())
        {
            var entity = await scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>()
                .Vehicles.SingleAsync();
            originalId = entity.Id;
            originalDate = entity.CreatedDate;
        }

        var response = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand(" ab123 gp ", "Toyota", "Corolla Cross", 2025, 25.135m,
                created.ConcurrencyStamp));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<UpSertVehicleResponse>())!;
        Assert.Equal("Updated vehicle AB123GP, make Toyota, model Corolla Cross.", updated.Message);
        Assert.NotEqual(created.ConcurrencyStamp, updated.ConcurrencyStamp);
        using var readScope = factory.Services.CreateScope();
        var stored = await readScope.ServiceProvider.GetRequiredService<LevelMovementDbContext>()
            .Vehicles.SingleAsync();
        Assert.Equal(originalId, stored.Id);
        Assert.Equal(originalDate, stored.CreatedDate);
        Assert.False(stored.IsDeleted);
        Assert.Equal(25.14m, stored.DailyRate.Amount);
        Assert.Equal(updated.ConcurrencyStamp, stored.ConcurrencyStamp);
        Assert.Equal(2, factory.SaveCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleStampReturnsConflictWithoutOverwritingEvenForUnchangedValues(bool unchanged)
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var created = await Insert(client);
        var change = new UpsertVehicleCommand(
            "AB123 GP", "Toyota", "Updated model", 2025, 20m, created.ConcurrencyStamp);
        var firstResponse = await client.PostAsJsonAsync("/api/vehicles", change);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var firstResult = (await firstResponse.Content.ReadFromJsonAsync<UpSertVehicleResponse>())!;
        var staleRequest = unchanged ? change : change with { Model = "Stale overwrite" };

        var staleResponse = await client.PostAsJsonAsync("/api/vehicles", staleRequest);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        var error = await staleResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Vehicle has changed.", error.GetProperty("title").GetString());
        using var scope = factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>()
            .Vehicles.SingleAsync();
        Assert.Equal("Updated model", stored.Model);
        Assert.Equal(firstResult.ConcurrencyStamp, stored.ConcurrencyStamp);
        Assert.Equal(2, factory.SaveCalls);
    }

    [Fact]
    public async Task ReturnedStampCanBeUsedForAnotherUpdate()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var created = await Insert(client);
        var first = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand("AB123 GP", "Toyota", "First edit", 2024, 10m,
                created.ConcurrencyStamp));
        var firstResult = (await first.Content.ReadFromJsonAsync<UpSertVehicleResponse>())!;
        var second = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand("AB123 GP", "Toyota", "Second edit", 2024, 10m,
                firstResult.ConcurrencyStamp));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondResult = (await second.Content.ReadFromJsonAsync<UpSertVehicleResponse>())!;
        Assert.NotEqual(firstResult.ConcurrencyStamp, secondResult.ConcurrencyStamp);
        Assert.Equal(3, factory.SaveCalls);
    }

    [Fact]
    public async Task MissingUpdateReturnsNotFoundWithoutInserting()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand("MISSING", "Toyota", "Corolla", 2024, 10m, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, factory.SaveCalls);
        using var scope = factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>()
            .Vehicles.AnyAsync());
    }

    [Fact]
    public async Task EmptyStampIsInvalidRatherThanAnInsert()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand("AB123 GP", "Toyota", "Corolla", 2024, 10m, Guid.Empty));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(errors.GetProperty("errors").TryGetProperty("concurrencyStamp", out _));
        Assert.Equal(0, factory.SaveCalls);
    }

    [Fact]
    public async Task DeletedVehicleIsNotUpdatedOrRestored()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var created = await Insert(client);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>();
            (await database.Vehicles.SingleAsync()).Delete();
            await database.SaveChangesAsync();
        }
        var response = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand("AB123 GP", "Toyota", "Edit", 2024, 10m,
                created.ConcurrencyStamp));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var checkScope = factory.Services.CreateScope();
        Assert.True((await checkScope.ServiceProvider.GetRequiredService<LevelMovementDbContext>()
            .Vehicles.IgnoreQueryFilters().SingleAsync()).IsDeleted);
        Assert.Equal(2, factory.SaveCalls);
    }

    private static async Task<UpSertVehicleResponse> Insert(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand("AB123 GP", "Toyota", "Corolla", 2024, 10m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UpSertVehicleResponse>())!;
    }

    public sealed class VehicleApiFactory : WebApplicationFactory<Program>
    {
        public const string TestApiKey = "LevelMovement.TestKey_Only";
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public int SaveCalls { get; private set; }

        private readonly Action<IServiceCollection>? _configure;
        private readonly string? _apiKey;
        public VehicleApiFactory(Action<IServiceCollection>? configure = null, string? apiKey = TestApiKey)
        {
            _configure = configure;
            _apiKey = apiKey;
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Security:ApiKey"] = _apiKey }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<LevelMovementDbContext>();
                services.AddScoped(_ =>
                {
                    var options = new DbContextOptionsBuilder<LevelMovementDbContext>()
                        .UseSqlite(_connection)
                        .AddInterceptors(new VehicleConcurrencyInterceptor())
                        .Options;
                    var context = new LevelMovementDbContext(options);
                    context.Database.EnsureCreated();
                    context.SavedChanges += (_, _) => SaveCalls++;
                    return context;
                });
                _configure?.Invoke(services);
            });
        }

        protected override void ConfigureClient(HttpClient client)
        {
            base.ConfigureClient(client);
            client.DefaultRequestHeaders.Add(ApiKeyOptions.HeaderName, TestApiKey);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection.Dispose();
        }
    }
}
