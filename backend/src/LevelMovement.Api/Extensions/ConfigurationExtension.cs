using FluentValidation;
using LevelMovement.Application.Behaviors;
using LevelMovement.Application.Interfaces.Persistence;
using LevelMovement.Application.Interfaces.Services;
using LevelMovement.Application.Vehicles.Commands;
using LevelMovement.Application.Vehicles.Organisers;
using LevelMovement.Application.Vehicles.Validators;
using LevelMovement.Infrastructure.Persistence;
using LevelMovement.Infrastructure.Persistence.Interceptors;
using LevelMovement.Infrastructure.Services;
using LevelMovement.Infrastructure.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LevelMovement.Api.Extensions;

public static class ConfigurationExtension
{
    public static void AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<VehicleConcurrencyInterceptor>();

        services.AddDbContext<LevelMovementDbContext>((provider, options) =>
        {
            var connectionString =
                configuration.GetConnectionString("LevelMovement")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:LevelMovement is missing.");

            options.UseSqlServer(connectionString);

            options.AddInterceptors(
                provider.GetRequiredService<VehicleConcurrencyInterceptor>());
        });
    }

    public static void ConfigureRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IVehicleRepository, VehicleRepository>();
    }

    public static void ConfigureServices(
        this IServiceCollection services)
    {
        services.AddScoped<IVehiclesService, VehiclesService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    public static void ConfigureCommands(
        this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<
                CreateVehicleCommand>();

            // Validate first; save only after the handler succeeds.
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddScoped<
            IValidator<CreateVehicleCommand>,
            CreateVehicleCommandValidator>();

        services.AddScoped<CreateVehicleOrganiser>();
    }
}