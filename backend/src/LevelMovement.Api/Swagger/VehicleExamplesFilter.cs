using System.Text.Json.Nodes;
using LevelMovement.Api.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LevelMovement.Api.Swagger;

public sealed class VehicleExamplesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(VehiclesController)) return;

        operation.Summary = "Create a vehicle";
        operation.Description = "Stores a new vehicle and returns its ID after saving. Registration is trimmed "
            + "and uppercased. Daily rate uses banker's rounding to two decimals. Year must have four digits. "
            + "Zero and negative rates are not prohibited by the current specification. "
            + "Requires a configured SQL Server database with the Vehicles schema.";

        if (operation.RequestBody?.Content is { } content)
        {
            foreach (var media in content.Values)
            {
                media.Examples = new Dictionary<string, IOpenApiExample>
                {
                    ["standard"] = new OpenApiExample
                    {
                        Summary = "Toyota Corolla",
                        Value = JsonNode.Parse("""{"registrationNumber":"CA 123-456","make":"Toyota","model":"Corolla","year":2024,"dailyRate":450.00}""")
                    },
                    ["rounding"] = new OpenApiExample
                    {
                        Summary = "Normalization and banker's rounding",
                        Description = "Registration becomes AB123 GP; daily rate becomes 10.12.",
                        Value = JsonNode.Parse("""{"registrationNumber":" ab123 gp ","make":"Volkswagen","model":"Polo","year":2023,"dailyRate":10.125}""")
                    }
                };
            }
        }

        AddResponseExample(operation, "201", """{"id":"019b2345-6789-7abc-8def-0123456789ab"}""");
        AddResponseExample(operation, "400", """{"title":"One or more validation errors occurred.","status":400,"errors":{"year":["'Year' must be between 1000 and 9999. You entered 999."]}}""");
        AddResponseExample(operation, "409", """{"title":"Registration number already exists.","status":409,"detail":"Use a different registration number. Deleted vehicles also retain their registration."}""");
    }

    private static void AddResponseExample(OpenApiOperation operation, string status, string json)
    {
        if (operation.Responses?.TryGetValue(status, out var response) == true &&
            response.Content is { } content)
        {
            foreach (var media in content.Values)
                media.Example = JsonNode.Parse(json);
        }
    }
}
