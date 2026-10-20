using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Application.Vehicles.Organisers;
using LevelMovement.Application.Vehicles.Queries;
using LevelMovement.Tests.TestDoubles;

namespace LevelMovement.Tests;

public sealed class VehicleQueryHandlerTests
{
    [Theory]
    [InlineData(1, 10)]
    [InlineData(3, 25)]
    public async Task GetVehiclesForwardsPagingAndCancellationAndReturnsTheApplicationResponse(int number, int size)
    {
        using var cancellation = new CancellationTokenSource();
        var expected = new VehiclePageResponse(
            [new("AB123GP", "Toyota", "Corolla", 2024, 100m, DateTime.UtcNow)], number, 5, 1);
        var service = new StubVehiclesService
        {
            GetPage = (pageNumber, pageSize, token) =>
            {
                Assert.Equal(number, pageNumber);
                Assert.Equal(size, pageSize);
                Assert.Equal(cancellation.Token, token);
                return Task.FromResult(expected);
            }
        };
        var handler = new GetVehiclesHandler(new VehicleReadOrganiser(service));

        var actual = await handler.Handle(new GetVehiclesQuery(number, size), cancellation.Token);

        Assert.Same(expected, actual);
        Assert.Equal(1, service.GetPageCalls);
    }

    [Fact]
    public async Task GetVehiclesPropagatesApplicationFailures()
    {
        var failure = new InvalidOperationException("The vehicle query failed.");
        var service = new StubVehiclesService { GetPage = (_, _, _) => Task.FromException<VehiclePageResponse>(failure) };
        var handler = new GetVehiclesHandler(new VehicleReadOrganiser(service));

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetVehiclesQuery(), CancellationToken.None));

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task GetVehiclesPreservesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new StubVehiclesService { GetPage = (_, _, token) => Task.FromCanceled<VehiclePageResponse>(token) };
        var handler = new GetVehiclesHandler(new VehicleReadOrganiser(service));

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.Handle(new GetVehiclesQuery(), cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
    }
}
