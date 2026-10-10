using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LevelMovement.Api.Controllers;
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

namespace LevelMovement.Tests;

public sealed class VehicleApiTests
{
    [Fact]
    public async Task CreateSavesNormalizedVehicleOnceBeforeReturningId()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/vehicles",
            new CreateVehicleCommand(" ab123 gp ", "Toyota", "Corolla", 2024, 10.125m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateVehicleResponse>();
        Assert.NotNull(result);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>();
        var vehicle = await context.Vehicles.SingleAsync();
        Assert.Equal(result.Id, vehicle.Id);
        Assert.Equal("AB123 GP", vehicle.RegistrationNumber.Value);
        Assert.Equal(10.12m, vehicle.DailyRate.Amount);
        Assert.NotEqual(Guid.Empty, vehicle.ConcurrencyStamp);
        Assert.False(vehicle.IsDeleted);
        Assert.Equal(1, factory.SaveCalls);
    }

    [Fact]
    public async Task InvalidCommandReturnsFieldErrorsWithoutSaving()
    {
        using var factory = new VehicleApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/vehicles",
            new CreateVehicleCommand("", "", "Corolla", 999, 10m));

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
        using var client = factory.CreateClient();
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var operation = document.GetProperty("paths").GetProperty("/api/vehicles").GetProperty("post");
        var examples = operation.GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("examples");
        Assert.Equal(450m, examples.GetProperty("standard").GetProperty("value").GetProperty("dailyRate").GetDecimal());
        Assert.Equal(10.125m, examples.GetProperty("rounding").GetProperty("value").GetProperty("dailyRate").GetDecimal());
        foreach (var status in new[] { "201", "400", "409" })
        {
            var content = operation.GetProperty("responses").GetProperty(status).GetProperty("content");
            Assert.All(content.EnumerateObject(), media => Assert.True(media.Value.TryGetProperty("example", out _)));
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }

    private sealed class VehicleApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public int SaveCalls { get; private set; }

        public VehicleApiFactory() => _connection.Open();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
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
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection.Dispose();
        }
    }
}
