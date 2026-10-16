using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace LevelMovement.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public static async Task<int> SeedAsync(
        LevelMovementDbContext database,
        CancellationToken cancellationToken = default)
    {
        // Deleted vehicles still reserve their registration number in the unique index.
        var registrations = await database.Vehicles
            .IgnoreQueryFilters()
            .Select(vehicle => vehicle.RegistrationNumber)
            .ToListAsync(cancellationToken);
        var existing = registrations.Select(number => number.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        (string Make, string Model, decimal Rate)[] fleet =
        [
            ("Toyota", "Corolla", 450m),
            ("Toyota", "Corolla Cross", 650m),
            ("Toyota", "Starlet", 350m),
            ("Toyota", "Hilux", 950m),
            ("Toyota", "Fortuner", 1100m),
            ("Volkswagen", "Polo", 400m),
            ("Volkswagen", "Polo Vivo", 325m),
            ("Volkswagen", "T-Cross", 625m),
            ("Volkswagen", "Tiguan", 900m),
            ("Volkswagen", "Amarok", 1050m),
            ("Suzuki", "Swift", 325m),
            ("Suzuki", "Baleno", 375m),
            ("Suzuki", "Fronx", 475m),
            ("Suzuki", "Jimny", 700m),
            ("Suzuki", "Ertiga", 600m),
            ("Hyundai", "Grand i10", 300m),
            ("Hyundai", "i20", 400m),
            ("Hyundai", "Venue", 525m),
            ("Hyundai", "Creta", 650m),
            ("Hyundai", "Tucson", 850m),
            ("Kia", "Picanto", 300m),
            ("Kia", "Rio", 400m),
            ("Kia", "Sonet", 525m),
            ("Kia", "Seltos", 675m),
            ("Kia", "Sportage", 900m),
            ("Ford", "Figo", 350m),
            ("Ford", "EcoSport", 550m),
            ("Ford", "Ranger", 950m),
            ("Ford", "Everest", 1150m),
            ("Nissan", "Micra", 350m),
            ("Nissan", "Magnite", 450m),
            ("Nissan", "Qashqai", 750m),
            ("Nissan", "Navara", 925m),
            ("Renault", "Kwid", 275m),
            ("Renault", "Clio", 400m),
            ("Renault", "Duster", 625m),
            ("Renault", "Triber", 475m),
            ("Haval", "Jolion", 650m),
            ("Haval", "H6", 825m),
            ("Chery", "Tiggo 4 Pro", 600m),
            ("Chery", "Tiggo 7 Pro", 775m),
            ("Mazda", "Mazda2", 400m),
            ("Mazda", "CX-3", 625m),
            ("Mazda", "CX-5", 850m),
            ("Honda", "Amaze", 375m),
            ("Honda", "Fit", 475m),
            ("Honda", "HR-V", 750m),
            ("Isuzu", "D-Max", 950m),
            ("BMW", "320i", 1250m),
            ("Mercedes-Benz", "C200", 1350m)
        ];

        var added = 0;
        var today = DateTime.UtcNow.Date;
        for (var index = 0; index < fleet.Length; index++)
        {
            var registration = $"DEMO{index + 1:000} GP";
            if (!existing.Add(registration)) continue;

            var item = fleet[index];
            database.Vehicles.Add(new Vehicle
            {
                Id = Guid.NewGuid(),
                RegistrationNumber = new RegistrationNumber(registration),
                Make = item.Make,
                Model = item.Model,
                Year = new VehicleYear(2020 + index % 6),
                DailyRate = new DailyRate(item.Rate),
                CreatedDate = today.AddDays(-index - 1).AddHours(9),
                ConcurrencyStamp = Guid.NewGuid(),
                IsDeleted = false
            });
            added++;
        }

        if (added > 0) await database.SaveChangesAsync(cancellationToken);
        return added;
    }
}
