using System.Net;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;
using LevelMovement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace LevelMovement.Tests;
public sealed class DefensiveProgrammingTests
{
    [Fact]
    public async Task HttpRequestsAreRejectedBeforeDatabaseWork()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/vehicles");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("HTTPS is required.", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, factory.SaveCalls);
    }
    [Fact]
    public async Task RepositoryRejectsMissingVehicleAndEmptyUpdateStamp()
    {
        await using var context = new LevelMovementDbContext(new DbContextOptionsBuilder<LevelMovementDbContext>().UseSqlite("Data Source=:memory:").Options);
        var repository = new VehicleRepository(context);
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => repository.Update(Vehicle.Register(new("TEST"), "Toyota", "Corolla", new(2024), new(100m)), Guid.Empty));
        Assert.Empty(context.ChangeTracker.Entries());
    }
    [Fact]
    public async Task RepositoryRejectsUpdatingAnUnloadedVehicle()
    {
        await using var context = new LevelMovementDbContext(new DbContextOptionsBuilder<LevelMovementDbContext>().UseSqlite("Data Source=:memory:").Options);
        var vehicle = Vehicle.Register(new("TEST"), "Toyota", "Corolla", new(2024), new(100m));
        Assert.Throws<InvalidOperationException>(() => new VehicleRepository(context).Update(vehicle, Guid.NewGuid()));
        Assert.Empty(context.ChangeTracker.Entries());
    }
    [Fact]
    public void DomainRejectsOversizedRatesAndMissingRegistrations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DailyRate(10000000000000000m));
        Assert.Throws<ArgumentNullException>(() => RegistrationNumber.Normalize(null!));
        Assert.Throws<ArgumentException>(() => RegistrationNumber.Normalize(" "));
    }
}
