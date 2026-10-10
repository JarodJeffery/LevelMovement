using LevelMovement.Api.Behaviors;
using LevelMovement.Application.Bookings.Organisers;
using LevelMovement.Application.Bookings.Queries;
using LevelMovement.Application.Vehicles.Queries;
using LevelMovement.Domain.Managers;
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
        services.AddScoped<IBookingEligibilityService, BookingEligibilityService>();
        services.AddScoped<VehicleBookingManager>();
    }

    public static void ConfigureCommands(
        this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<
                UpsertVehicleCommand>();

            configuration.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddScoped<
            IValidator<UpsertVehicleCommand>,
            UpsertVehicleCommandValidator>();

        services.AddScoped<UpsertVehicleOrganiser>();
        services.AddScoped<VehicleReadOrganiser>();
        services.AddScoped<DeleteVehicleOrganiser>();
        services.AddScoped<BookingEligibilityOrganiser>();
        services.AddScoped<IValidator<GetVehicleDetailsQuery>, GetVehicleDetailsValidator>();
        services.AddScoped<IValidator<DeleteVehicleCommand>, DeleteVehicleValidator>();
        services.AddScoped<IValidator<ValidateBookingQuery>, ValidateBookingValidator>();
    }
}
