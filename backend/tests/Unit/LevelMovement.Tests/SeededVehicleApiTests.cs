using System.Net;
using System.Net.Http.Json;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LevelMovement.Tests;

public sealed class SeededVehicleApiTests
{
    [Fact]
    public async Task EverySeededVehicleCanBeEditedWithItsDetailsStamp()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>();
            Assert.Equal(50, await DemoDataSeeder.SeedAsync(database));
        }

        var fleet = new List<VehicleSummaryResponse>();
        for (var pageNumber = 1; pageNumber <= 5; pageNumber++)
        {
            var page = await client.GetFromJsonAsync<VehiclePageResponse>($"/api/vehicles?pageNumber={pageNumber}&pageSize=10");
            Assert.NotNull(page);
            Assert.Equal(pageNumber, page.PageNumber);
            Assert.Equal(5, page.TotalPages);
            Assert.Equal(10, page.Count);
            Assert.Equal(10, page.Items.Count);
            fleet.AddRange(page.Items);
        }
        Assert.Equal(50, fleet.Select(vehicle => vehicle.RegistrationNumber).Distinct().Count());

        foreach (var vehicle in fleet)
        {
            var path = $"/api/vehicles/{Uri.EscapeDataString(vehicle.RegistrationNumber)}";
            var before = await client.GetFromJsonAsync<VehicleDetailsResponse>(path);
            Assert.NotNull(before);
            Assert.NotEqual(Guid.Empty, before.ConcurrencyStamp);

            var updatedModel = before.Model + " demo edit";
            var updatedRate = before.DailyRate + 0.01m;
            using var response = await client.PostAsJsonAsync("/api/vehicles",
                new UpsertVehicleCommand(before.RegistrationNumber, before.Make, updatedModel,
                    before.Year, updatedRate, before.ConcurrencyStamp));
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"Editing {before.RegistrationNumber} returned {(int)response.StatusCode}: " +
                await response.Content.ReadAsStringAsync());

            var saved = await response.Content.ReadFromJsonAsync<UpSertVehicleResponse>();
            Assert.NotNull(saved);
            Assert.NotEqual(before.ConcurrencyStamp, saved.ConcurrencyStamp);
            var after = await client.GetFromJsonAsync<VehicleDetailsResponse>(path);
            Assert.NotNull(after);
            Assert.Equal(updatedModel, after.Model);
            Assert.Equal(updatedRate, after.DailyRate);
            Assert.Equal(before.CreatedDate, after.CreatedDate);
            Assert.Equal(saved.ConcurrencyStamp, after.ConcurrencyStamp);
        }

        Assert.Equal(5, (await client.GetFromJsonAsync<VehiclePageResponse>("/api/vehicles"))!.TotalPages);
    }
}
