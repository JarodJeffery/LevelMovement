using System.Text.Json.Nodes;
using LevelMovement.Api.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace LevelMovement.Api.Swagger;
public sealed class VehicleExamplesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(VehiclesController) && context.MethodInfo.DeclaringType != typeof(BookingsController)) return;
        var method = context.MethodInfo.Name;
        operation.Summary = method switch
        {
            "Upsert" => "Insert or update a vehicle",
            "GetAll" => "List active vehicles without stamps",
            "GetDetails" => "Get vehicle details and its edit/delete stamp",
            "Delete" => "Soft-delete a vehicle using its latest stamp",
            _ => "Check booking eligibility without saving a booking"
        };
        operation.Description = method switch
        {
            "Upsert" => "Omit the stamp to insert (201); supply the latest detail or save response stamp to update (200). Registration stays fixed. Whitespace is ignored and registrations are stored and returned in uppercase without spaces (KW 00 KB GP becomes KW00KBGP). Rates use banker’s rounding.",
            "GetAll" => "Deleted vehicles are excluded. No IDs or stamps are returned.",
            "GetDetails" => "Registration lookup requires no input stamp. Return the stamp to save or delete. Deleted vehicles return 404.",
            "Delete" => "Send the latest stamp in the JSON body. Stale stamps return 409; deleted or missing vehicles return 404.",
            _ => "Active vehicles return 200, deleted vehicles 409, unknown registrations 404. This is a read-only eligibility demo."
        };
        if (operation.RequestBody?.Content is { } requestContent)
        {
            foreach (var media in requestContent.Values)
            {
                if (method == "Upsert")
                    media.Examples = new Dictionary<string, IOpenApiExample>
                    {
                        ["standard"] = Example("Insert", """{"registrationNumber":"KW00KBGP","make":"Toyota","model":"Corolla","year":2024,"dailyRate":450}"""),
                        ["rounding"] = Example("Rounds to 10.12", """{"registrationNumber":" ab123 gp ","make":"Volkswagen","model":"Polo","year":2023,"dailyRate":10.125}"""),
                        ["update"] = Example("Replace with the latest saved stamp", """{"registrationNumber":"KW00KBGP","make":"Toyota","model":"Corolla","year":2024,"dailyRate":400,"concurrencyStamp":"823dff0e-1ab9-41cb-8f24-4d4ee02a7ae2"}""")
                    };
                else media.Example = JsonNode.Parse(method == "Delete"
                    ? """{"concurrencyStamp":"823dff0e-1ab9-41cb-8f24-4d4ee02a7ae2"}"""
                    : """{"registrationNumber":"KW00KBGP"}""");
            }
        }
        var vehicle = JsonNode.Parse("""{"registrationNumber":"KW00KBGP","make":"Toyota","model":"Corolla","year":2024,"dailyRate":450,"createdDate":"2026-10-10T12:00:00Z"}""")!;
        if (method == "GetDetails") vehicle["concurrencyStamp"] = "823dff0e-1ab9-41cb-8f24-4d4ee02a7ae2";
        if (operation.Responses is null) return;
        foreach (var (status, response) in operation.Responses)
        {
            if (response.Content is null) continue;
            JsonNode example = status switch
            {
                "400" => JsonNode.Parse("""{"title":"One or more validation errors occurred.","status":400,"errors":{"registrationNumber":["Registration number is required."]}}""")!,
                "404" => JsonNode.Parse("""{"title":"Vehicle was not found.","status":404,"detail":"No vehicle is available for this operation."}""")!,
                "409" => JsonNode.Parse(method == "Validate" ? """{"title":"Vehicle cannot be booked.","status":409,"detail":"Deleted vehicles cannot be booked."}""" : """{"title":"Vehicle has changed.","status":409,"detail":"Reload the vehicle and use its latest concurrency stamp before updating or deleting."}""")!,
                "500" => JsonNode.Parse("""{"title":"An unexpected error occurred.","status":500,"detail":"Please try again. If the problem continues, contact support."}""")!,
                _ when method == "GetAll" => new JsonArray(vehicle.DeepClone()),
                _ when method == "GetDetails" => vehicle.DeepClone(),
                _ when method == "Validate" => JsonNode.Parse("""{"registrationNumber":"KW00KBGP","eligible":true,"message":"Vehicle KW00KBGP is eligible for booking. No booking has been created."}""")!,
                _ => JsonNode.Parse("""{"registrationNumber":"KW00KBGP","make":"Toyota","model":"Corolla","message":"Updated vehicle KW00KBGP, make Toyota, model Corolla.","concurrencyStamp":"823dff0e-1ab9-41cb-8f24-4d4ee02a7ae2"}""")!
            };
            if (status == "201") example["message"] = "Registered vehicle KW00KBGP, make Toyota, model Corolla.";
            foreach (var media in response.Content.Values) media.Example = example.DeepClone();
        }
    }
    private static OpenApiExample Example(string summary, string json) => new() { Summary = summary, Value = JsonNode.Parse(json) };
}
