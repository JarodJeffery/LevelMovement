using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;
using LevelMovement.Infrastructure.Persistence;
using LevelMovement.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.Validators;

namespace LevelMovement.Tests;

public sealed class DemoDataSeederTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly LevelMovementDbContext _database;

    public DemoDataSeederTests()
    {
        _connection.Open();
        _database = new LevelMovementDbContext(
            new DbContextOptionsBuilder<LevelMovementDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(new VehicleConcurrencyInterceptor())
                .Options);
        _database.Database.EnsureCreated();
    }

    [Fact]
    public async Task SeedsFiftyUsableVehiclesWithUniqueRegistrations()
    {
        Assert.Equal(50, await DemoDataSeeder.SeedAsync(_database));
        _database.ChangeTracker.Clear();
        var vehicles = await _database.Vehicles.ToListAsync();

        Assert.Equal(50, vehicles.Count);
        Assert.Equal(50, vehicles.Select(vehicle => vehicle.RegistrationNumber.Value).Distinct().Count());
        Assert.True(vehicles.Select(vehicle => vehicle.Make).Distinct().Count() > 10);
        Assert.Equal(2023, vehicles.Single(vehicle => vehicle.Model == "Fronx").Year.Value);
        Assert.All(vehicles, vehicle =>
        {
            Assert.NotEqual(Guid.Empty, vehicle.Id);
            Assert.NotEqual(Guid.Empty, vehicle.ConcurrencyStamp);
            Assert.InRange(vehicle.Year.Value, 2020, 2025);
            Assert.True(vehicle.DailyRate.Amount > 0);
            Assert.True(vehicle.CreatedDate < DateTime.UtcNow);
            Assert.False(vehicle.IsDeleted);
            var validation = new UpsertVehicleCommandValidator().Validate(new UpsertVehicleCommand(
                vehicle.RegistrationNumber.Value, vehicle.Make, vehicle.Model, vehicle.Year.Value,
                vehicle.DailyRate.Amount, vehicle.ConcurrencyStamp));
            Assert.True(validation.IsValid, validation.ToString());
        });
    }

    [Fact]
    public async Task RerunPreservesEditsAndUserDataAndSkipsDeletedRegistrations()
    {
        await DemoDataSeeder.SeedAsync(_database);
        var vehicles = await _database.Vehicles.OrderBy(vehicle => vehicle.Id).ToListAsync();
        var edited = vehicles[0];
        edited.UpdateDetails(edited.Make, edited.Model, edited.Year, new DailyRate(999.99m));
        var deleted = vehicles[1];
        deleted.Delete();
        var userVehicle = CreateVehicle("USER123 GP");
        _database.Vehicles.Add(userVehicle);
        await _database.SaveChangesAsync();
        var originalStamp = edited.ConcurrencyStamp;
        _database.ChangeTracker.Clear();

        Assert.Equal(0, await DemoDataSeeder.SeedAsync(_database));
        Assert.Equal(51, await _database.Vehicles.IgnoreQueryFilters().CountAsync());
        Assert.Equal(50, await _database.Vehicles.CountAsync());
        var savedEdit = await _database.Vehicles.SingleAsync(vehicle => vehicle.Id == edited.Id);
        Assert.Equal(999.99m, savedEdit.DailyRate.Amount);
        Assert.Equal(originalStamp, savedEdit.ConcurrencyStamp);
        Assert.True((await _database.Vehicles.IgnoreQueryFilters()
            .SingleAsync(vehicle => vehicle.Id == deleted.Id)).IsDeleted);
        Assert.True(await _database.Vehicles.AnyAsync(vehicle => vehicle.Id == userVehicle.Id));
    }

    [Fact]
    public async Task AddsOnlyMissingDemoRegistrationsWhenExistingPlateIsCompact()
    {
        var existing = CreateVehicle("DEMO001GP");
        _database.Vehicles.Add(existing);
        await _database.SaveChangesAsync();
        _database.ChangeTracker.Clear();

        Assert.Equal(49, await DemoDataSeeder.SeedAsync(_database));
        Assert.Equal(50, await _database.Vehicles.CountAsync());
        Assert.Equal("Existing", (await _database.Vehicles
            .SingleAsync(vehicle => vehicle.Id == existing.Id)).Make);
    }

    private static Vehicle CreateVehicle(string registration) => Vehicle.Register(
        new RegistrationNumber(registration),
        "Existing",
        "Vehicle",
        new VehicleYear(2024),
        new DailyRate(500m));

    public void Dispose()
    {
        _database.Dispose();
        _connection.Dispose();
    }
}
