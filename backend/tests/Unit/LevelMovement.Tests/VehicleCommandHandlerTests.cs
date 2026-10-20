using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.Organisers;
using LevelMovement.Application.Vehicles.Results;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;
using LevelMovement.Tests.TestDoubles;

namespace LevelMovement.Tests;

public sealed class VehicleCommandHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpsertRoutesCreateAndUpdateWithNormalizedValuesAndExpectedStamp(bool update)
    {
        using var cancellation = new CancellationTokenSource();
        var stamp = Guid.NewGuid();
        var vehicle = Vehicle.Register(new("AB123GP"), "Toyota", "Corolla", new(2024), new(10.12m));
        var expected = new UpsertVehicleResult(vehicle, isCreated: !update);
        var service = new StubVehiclesService();

        void AssertValues(RegistrationNumber registration, string make, string model,
            VehicleYear year, DailyRate rate, CancellationToken token)
        {
            Assert.Equal("AB123GP", registration.Value);
            Assert.Equal("Toyota", make);
            Assert.Equal("Corolla", model);
            Assert.Equal(2024, year.Value);
            Assert.Equal(10.12m, rate.Amount);
            Assert.Equal(cancellation.Token, token);
        }

        service.Register = (registration, make, model, year, rate, token) =>
        {
            AssertValues(registration, make, model, year, rate, token);
            return Task.FromResult(expected);
        };
        service.Update = (registration, make, model, year, rate, expectedStamp, token) =>
        {
            AssertValues(registration, make, model, year, rate, token);
            Assert.Equal(stamp, expectedStamp);
            return Task.FromResult(expected);
        };

        var handler = new UpsertVehicleCommandHandler(new UpsertVehicleOrganiser(service));
        var command = new UpsertVehicleCommand(" ab 123 gp ", "Toyota", "Corolla", 2024,
            10.125m, update ? stamp : null);
        var result = await handler.Handle(command, cancellation.Token);

        Assert.Same(expected, result);
        Assert.Equal(!update, result.IsCreated);
        Assert.Equal(update ? 0 : 1, service.RegisterCalls);
        Assert.Equal(update ? 1 : 0, service.UpdateCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpsertPropagatesApplicationFailures(bool update)
    {
        var failure = new InvalidOperationException("The application operation failed.");
        var service = new StubVehiclesService
        {
            Register = (_, _, _, _, _, _) => Task.FromException<UpsertVehicleResult>(failure),
            Update = (_, _, _, _, _, _, _) => Task.FromException<UpsertVehicleResult>(failure)
        };
        var handler = new UpsertVehicleCommandHandler(new UpsertVehicleOrganiser(service));
        var command = new UpsertVehicleCommand("AB123GP", "Toyota", "Corolla", 2024, 100m,
            update ? Guid.NewGuid() : null);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Same(failure, actual);
    }
}
