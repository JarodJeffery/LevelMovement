using LevelMovement.Api.Middleware;
using LevelMovement.Api.Swagger;
using LevelMovement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using LevelMovement.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

var migrateOnly = args.Contains("--migrate");
var builder = WebApplication.CreateBuilder(
    args.Where(argument => argument != "--migrate").ToArray());

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = actionContext =>
    {
        var context = actionContext.HttpContext;
        var problem = new ValidationProblemDetails(actionContext.ModelState) { Status = 400 };
        context.RequestServices.GetRequiredService<ILogger<ApiExceptionHandler>>().LogWarning(
            "Model binding failed. TraceId {TraceId} Endpoint {Endpoint} CqrsRequest {CqrsRequest} FailingFunction {FailingFunction} StatusCode {StatusCode}",
            context.TraceIdentifier, context.GetEndpoint()?.DisplayName, "ModelBinding", context.GetEndpoint()?.DisplayName, 400);
        return new BadRequestObjectResult(problem);
    };
});
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions.Remove("traceId"));
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Level Movement Vehicles API",
        Version = "v1",
        Description = "Vehicle CRUD and booking eligibility demo with example requests and responses."
    });
    options.OperationFilter<VehicleExamplesFilter>();
});
builder.Services.AddDatabase(builder.Configuration);
builder.Services.ConfigureRepositories();
builder.Services.ConfigureServices();
builder.Services.ConfigureCommands();

var app = builder.Build();

if (migrateOnly)
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<LevelMovementDbContext>();
    await database.Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.Use(async (context, next) =>
{
    if (!context.Request.IsHttps)
    {
        context.RequestServices.GetRequiredService<ILogger<Program>>().LogWarning(
            "Rejected non-HTTPS request. TraceId {TraceId} Endpoint {Endpoint}", context.TraceIdentifier, context.Request.Path);
        await Results.Problem(statusCode: 400, title: "HTTPS is required.", detail: "Use the HTTPS address for this service.").ExecuteAsync(context);
        return;
    }
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next(context);
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehicles API v1"));
}

app.MapControllers();

app.Run();

public partial class Program;

