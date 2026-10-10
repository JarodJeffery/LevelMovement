using FluentValidation;
using LevelMovement.Api.Middleware;
using LevelMovement.Api.Swagger;
using LevelMovement.Application.Behaviors;
using LevelMovement.Application.Interfaces.Persistence;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.Organisers;
using LevelMovement.Application.Vehicles.Validators;
using LevelMovement.Infrastructure.Persistence;
using LevelMovement.Infrastructure.Persistence.Interceptors;
using LevelMovement.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using LevelMovement.Api.Extensions;

var migrateOnly = args.Contains("--migrate");
var builder = WebApplication.CreateBuilder(
    args.Where(argument => argument != "--migrate").ToArray());

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Level Movement Vehicles API",
        Version = "v1",
        Description = "Vehicle creation API with example requests and responses."
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vehicles API v1"));
}

app.MapControllers();

app.Run();

public partial class Program;

