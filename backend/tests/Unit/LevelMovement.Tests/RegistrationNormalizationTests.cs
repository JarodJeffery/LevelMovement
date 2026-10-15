using System.Net;
using System.Net.Http.Json;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Application.Vehicles.Validators;
using LevelMovement.Application.Vehicles.Queries;
using LevelMovement.Application.Bookings.Queries;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LevelMovement.Tests;

public sealed class RegistrationNormalizationTests
{
    [Fact]
    public async Task SpacedAndCompactRegistrationsAddressTheSameStoredVehicle()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var created = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand(" kw 00 kb gp ", "Toyota", "Corolla", 2024, 100m));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var saved = (await created.Content.ReadFromJsonAsync<UpSertVehicleResponse>())!;
        Assert.Equal("KW00KBGP", saved.RegistrationNumber);

        foreach (var registration in new[] { "KW00KBGP", "KW 00 KB GP", "kw\t00\u00a0kb gp" })
        {
            var details = await client.GetFromJsonAsync<VehicleDetailsResponse>(
                $"/api/vehicles/{Uri.EscapeDataString(registration)}");
            Assert.Equal("KW00KBGP", details!.RegistrationNumber);
            Assert.Equal(saved.ConcurrencyStamp, details.ConcurrencyStamp);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/bookings/validate",
                new ValidateBookingQuery(registration))).StatusCode);
        }

        var update = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand("KW00KBGP", "Toyota", "Yaris", 2024, 100m, saved.ConcurrencyStamp));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<UpSertVehicleResponse>())!;
        var deleted = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/vehicles/KW%2000%20KB%20GP")
        {
            Content = JsonContent.Create(new DeleteVehicleRequest(updated.ConcurrencyStamp))
        });
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/vehicles/KW00KBGP")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/bookings/validate",
            new ValidateBookingQuery("KW 00 KB GP"))).StatusCode);

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>();
        Assert.Equal("KW00KBGP", await database.Database.SqlQueryRaw<string>(
            "SELECT RegistrationNumber AS Value FROM Vehicles").SingleAsync());
    }

    [Fact]
    public async Task UniqueIndexRejectsSpacingVariantsIncludingDeletedVehicles()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/vehicles",
            new UpsertVehicleCommand("KW00KBGP", "Toyota", "Corolla", 2024, 100m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>();
        var original = await database.Vehicles.SingleAsync();
        original.Delete();
        await database.SaveChangesAsync();
        database.Vehicles.Add(LevelMovement.Domain.Vehicles.Vehicle.Register(
            new("KW 00 KB GP"), "Ford", "Fiesta", new(2024), new(100m)));
        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [Fact]
    public void AllRequestValidatorsMeasureTheCompactRegistration()
    {
        var spaced = string.Join(" ", Enumerable.Repeat("A", 20));
        Assert.True(new UpsertVehicleCommandValidator().Validate(new UpsertVehicleCommand(spaced, "Toyota", "Corolla", 2024, 100m)).IsValid);
        Assert.True(new GetVehicleDetailsValidator().Validate(new GetVehicleDetailsQuery(spaced)).IsValid);
        Assert.True(new DeleteVehicleValidator().Validate(new DeleteVehicleCommand(spaced, Guid.NewGuid())).IsValid);
        Assert.True(new ValidateBookingValidator().Validate(new ValidateBookingQuery(spaced)).IsValid);
        Assert.False(new ValidateBookingValidator().Validate(new ValidateBookingQuery(spaced + "A")).IsValid);
        Assert.False(new ValidateBookingValidator().Validate(new ValidateBookingQuery(" \t\u00a0")).IsValid);
    }
}
