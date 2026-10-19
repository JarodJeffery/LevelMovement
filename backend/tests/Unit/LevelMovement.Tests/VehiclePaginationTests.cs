using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Domain.Vehicles;
using LevelMovement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LevelMovement.Tests;

public sealed class VehiclePaginationTests
{
    private static HttpClient Client(VehicleApiTests.VehicleApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    [Fact]
    public async Task PagesHaveStableOrderingNoOverlapAndActiveOnlyTotals()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = Client(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>();
            // Insert in reverse order so the database insertion order cannot satisfy the assertion.
            for (var i = 26; i >= 1; i--)
            {
                var vehicle = Vehicle.Register(new($"PAGE{i:000}"), "Toyota", "Corolla", new(2024), new(450));
                if (i % 7 == 0) vehicle.Delete();
                db.Vehicles.Add(vehicle);
            }
            await db.SaveChangesAsync();
        }
        var expected = Enumerable.Range(1, 26).Where(i => i % 7 != 0).Select(i => $"PAGE{i:000}").ToArray();
        var seen = new List<string>();
        for (var number = 1; number <= 3; number++)
        {
            var page = (await client.GetFromJsonAsync<VehiclePageResponse>($"/api/vehicles?pageNumber={number}"))!;

            Assert.Equal(number, page.PageNumber);
            Assert.Equal(3, page.TotalPages);
            Assert.Equal(number < 3 ? 10 : 3, page.Count);

            Assert.Equal(expected.Skip((number - 1) * 10).Take(10), page.Items.Select(v => v.RegistrationNumber));
            seen.AddRange(page.Items.Select(v => v.RegistrationNumber));
        }
        Assert.Equal(expected, seen);
        Assert.Equal(seen.Count, seen.Distinct().Count());
        var repeat = (await client.GetFromJsonAsync<VehiclePageResponse>("/api/vehicles"))!;
        Assert.Equal(expected.Take(10), repeat.Items.Select(v => v.RegistrationNumber));
        var single = (await client.GetFromJsonAsync<VehiclePageResponse>("/api/vehicles?pageSize=1"))!;
        Assert.Single(single.Items);
        Assert.Equal(1, single.Count);
        Assert.Equal(23, single.TotalPages);
        var custom = (await client.GetFromJsonAsync<VehiclePageResponse>("/api/vehicles?pageSize=25"))!;

        Assert.Equal(23, custom.Items.Count);
        Assert.Equal(23, custom.Count);
        Assert.Equal(1, custom.TotalPages);

        var maximum = (await client.GetFromJsonAsync<VehiclePageResponse>("/api/vehicles?pageSize=100"))!;
        Assert.Equal(23, maximum.Items.Count);
        foreach (var url in new[] { "?pageNumber=4", "?pageNumber=2147483647&pageSize=100" })
        {
            var page = (await client.GetFromJsonAsync<VehiclePageResponse>("/api/vehicles" + url))!;
            Assert.Empty(page.Items);
            Assert.Equal(0, page.Count);
            Assert.Equal(url.Contains("pageSize=100") ? 1 : 3, page.TotalPages);

        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/vehicles/PAGE007")).StatusCode);
    }

    [Fact]
    public async Task EmptyFleetHasZeroCountIncludingOutOfRangeRequests()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = Client(factory);
        foreach (var url in new[] { "/api/vehicles", "/api/vehicles?pageNumber=99&pageSize=1" })
        {
            var page = (await client.GetFromJsonAsync<VehiclePageResponse>(url))!;
            Assert.Empty(page.Items);
            Assert.Equal(0, page.Count);
            Assert.Equal(0, page.TotalPages);

        }
    }

    [Theory]
    [InlineData("pageNumber=0")]
    [InlineData("pageNumber=-1")]
    [InlineData("pageNumber=abc")]
    [InlineData("pageNumber=1.5")]
    [InlineData("pageNumber=2147483648")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=-1")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=abc")]
    public async Task InvalidParametersReturn400WithoutSaving(string query)
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = Client(factory);
        var response = await client.GetAsync("/api/vehicles?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("errors", out _));
        Assert.Equal(0, factory.SaveCalls);
    }

    [Fact]
    public async Task SwaggerDescribesPagingParametersAndResponseEnvelope()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = Client(factory);
        var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var operation = swagger.GetProperty("paths").GetProperty("/api/vehicles").GetProperty("get");
        var parameters = operation.GetProperty("parameters").EnumerateArray().ToArray();
        Assert.Equal(new[] { "pageNumber", "pageSize" }, parameters.Select(p => p.GetProperty("name").GetString()));
        Assert.Equal(1, parameters[0].GetProperty("schema").GetProperty("default").GetInt32());
        Assert.Equal(10, parameters[1].GetProperty("schema").GetProperty("default").GetInt32());
        Assert.Equal(100, parameters[1].GetProperty("schema").GetProperty("maximum").GetInt32());
        var example = operation.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("example");
        Assert.Equal(1, example.GetProperty("count").GetInt32());
        Assert.Equal(1, example.GetProperty("items").GetArrayLength());
        Assert.Equal(1, example.GetProperty("pageNumber").GetInt32());
        Assert.Equal(1, example.GetProperty("totalPages").GetInt32());
        Assert.Equal(4, example.EnumerateObject().Count());
        var schema = swagger.GetProperty("components").GetProperty("schemas").GetProperty("VehiclePageResponse").GetProperty("properties");
        Assert.All(new[] { "items", "pageNumber", "totalPages", "count" }, name => Assert.True(schema.TryGetProperty(name, out _)));
    }
}
