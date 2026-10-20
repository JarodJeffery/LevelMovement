using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Concurrent;
using LevelMovement.Api.Security;
using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.DTOs;
using LevelMovement.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace LevelMovement.Tests;

public sealed class ApiKeySecurityTests
{
    private static HttpClient Client(VehicleApiTests.VehicleApiFactory factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Remove(ApiKeyOptions.HeaderName);
        return client;
    }

    private static VehicleApiTests.VehicleApiFactory Factory(StubVehiclesService service)
        => new(services =>
        {
            services.RemoveAll<IVehiclesService>();
            services.AddSingleton<IVehiclesService>(service);
        });

    private static async Task AssertUnauthorized(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ApiKeyOptions.Scheme, response.Headers.WwwAuthenticate.Single().Scheme);
        var text = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(text);
        Assert.Equal("Unauthorized", problem.GetProperty("title").GetString());
        Assert.Equal(401, problem.GetProperty("status").GetInt32());
        Assert.Equal("A valid API key is required.", problem.GetProperty("detail").GetString());
        Assert.False(problem.TryGetProperty("traceId", out _));
        Assert.DoesNotContain(VehicleApiTests.VehicleApiFactory.TestApiKey, text);
    }

    [Theory]
    [InlineData("GET", "/api/vehicles")]
    [InlineData("GET", "/api/vehicles/MISSING")]
    [InlineData("POST", "/api/vehicles")]
    [InlineData("DELETE", "/api/vehicles/MISSING")]
    [InlineData("POST", "/api/bookings/validate")]
    public async Task MissingKeyBlocksEveryEndpointBeforeApplicationWork(string method, string path)
    {
        var service = new StubVehiclesService();
        using var factory = Factory(service);
        using var client = Client(factory);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "DELETE") request.Content = JsonContent.Create(new { });

        using var response = await client.SendAsync(request);

        await AssertUnauthorized(response);
        Assert.Equal(0, service.GetPageCalls + service.RegisterCalls + service.UpdateCalls);
        Assert.Equal(0, factory.SaveCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-key")]
    [InlineData("levelmovement.testkey_only")]
    [InlineData(VehicleApiTests.VehicleApiFactory.TestApiKey + "," + VehicleApiTests.VehicleApiFactory.TestApiKey)]
    public async Task EmptyIncorrectAndCombinedKeysAreRejected(string supplied)
    {
        var service = new StubVehiclesService();
        using var factory = Factory(service);
        using var client = Client(factory);
        Assert.True(client.DefaultRequestHeaders.TryAddWithoutValidation(ApiKeyOptions.HeaderName, supplied));

        using var response = await client.GetAsync("/api/vehicles");

        await AssertUnauthorized(response);
        Assert.Equal(0, service.GetPageCalls);
    }

    [Theory]
    [InlineData(VehicleApiTests.VehicleApiFactory.TestApiKey)]
    [InlineData("invalid-key")]
    public async Task DuplicateHeadersAreRejectedEvenIfAKeyIsValid(string second)
    {
        var service = new StubVehiclesService();
        using var factory = Factory(service);
        using var client = Client(factory);
        Assert.True(client.DefaultRequestHeaders.TryAddWithoutValidation(ApiKeyOptions.HeaderName,
            new[] { VehicleApiTests.VehicleApiFactory.TestApiKey, second }));

        using var response = await client.GetAsync("/api/vehicles");

        await AssertUnauthorized(response);
        Assert.Equal(0, service.GetPageCalls);
    }

    [Theory]
    [InlineData("X-Api-Key")]
    [InlineData("x-api-key")]
    public async Task ValidKeyAllowsTheApplicationOperation(string header)
    {
        var service = new StubVehiclesService { GetPage = (_, _, _) => Task.FromResult(new VehiclePageResponse([], 1, 0, 0)) };
        using var factory = Factory(service);
        using var client = Client(factory);
        client.DefaultRequestHeaders.Add(header, VehicleApiTests.VehicleApiFactory.TestApiKey);

        using var response = await client.GetAsync("/api/vehicles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, service.GetPageCalls);
        Assert.Equal(0, factory.SaveCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingOrBlankConfiguredKeyPreventsStartup(string? key)
    {
        using var factory = new VehicleApiTests.VehicleApiFactory(apiKey: key);

        var failure = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("Security:ApiKey must be configured", failure.Message);
    }

    [Fact]
    public async Task AuthenticationDoesNotExposeConfiguredOrSuppliedKeysInResponsesOrLogs()
    {
        const string supplied = "Private-supplied-key-that-must-not-be-logged";
        using var logs = new CapturedLogs();
        var service = new StubVehiclesService { GetPage = (_, _, _) => Task.FromResult(new VehiclePageResponse([], 1, 0, 0)) };
        using var factory = new VehicleApiTests.VehicleApiFactory(services =>
        {
            services.RemoveAll<IVehiclesService>();
            services.AddSingleton<IVehiclesService>(service);
            services.AddLogging(logging => logging.AddProvider(logs));
        });
        using var client = Client(factory);
        client.DefaultRequestHeaders.Add(ApiKeyOptions.HeaderName, supplied);
        using var rejected = await client.GetAsync("/api/vehicles");
        await AssertUnauthorized(rejected);
        Assert.DoesNotContain(supplied, await rejected.Content.ReadAsStringAsync());

        client.DefaultRequestHeaders.Remove(ApiKeyOptions.HeaderName);
        client.DefaultRequestHeaders.Add(ApiKeyOptions.HeaderName, VehicleApiTests.VehicleApiFactory.TestApiKey);
        using var accepted = await client.GetAsync("/api/vehicles");
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.DoesNotContain(VehicleApiTests.VehicleApiFactory.TestApiKey, await accepted.Content.ReadAsStringAsync());
        Assert.NotEmpty(logs.Messages);
        Assert.All(logs.Messages, message =>
        {
            Assert.DoesNotContain(supplied, message);
            Assert.DoesNotContain(VehicleApiTests.VehicleApiFactory.TestApiKey, message);
        });
    }

    [Fact]
    public async Task SwaggerIsAnonymousAndDocumentsAuthenticationForAllOperations()
    {
        using var factory = new VehicleApiTests.VehicleApiFactory();
        using var client = Client(factory);
        using var ui = await client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, ui.StatusCode);
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var scheme = document.GetProperty("components").GetProperty("securitySchemes").GetProperty(ApiKeyOptions.Scheme);
        Assert.Equal("apiKey", scheme.GetProperty("type").GetString());
        Assert.Equal("header", scheme.GetProperty("in").GetString());
        Assert.Equal(ApiKeyOptions.HeaderName, scheme.GetProperty("name").GetString());
        Assert.Equal(0, document.GetProperty("security")[0].GetProperty(ApiKeyOptions.Scheme).GetArrayLength());

        foreach (var path in document.GetProperty("paths").EnumerateObject())
        foreach (var operation in path.Value.EnumerateObject())
        {
            var unauthorized = operation.Value.GetProperty("responses").GetProperty("401");
            var example = unauthorized.GetProperty("content").GetProperty("application/json").GetProperty("example");
            Assert.Equal(401, example.GetProperty("status").GetInt32());
        }
    }

    private sealed class CapturedLogs : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(Messages);
        public void Dispose() { }

        private sealed class CaptureLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                messages.Enqueue(formatter(state, exception));
                if (exception is not null) messages.Enqueue(exception.ToString());
            }
        }
    }
}
