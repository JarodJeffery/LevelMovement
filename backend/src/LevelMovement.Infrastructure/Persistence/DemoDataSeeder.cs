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

        (string Make, string Model, int Year, decimal Rate)[] fleet =
        [
            ("Toyota", "Corolla", 2020, 450m),
            ("Toyota", "Corolla Cross", 2021, 650m),
            ("Toyota", "Starlet", 2022, 350m),
            ("Toyota", "Hilux", 2023, 950m),
            ("Toyota", "Fortuner", 2024, 1100m),
            ("Volkswagen", "Polo", 2025, 400m),
            ("Volkswagen", "Polo Vivo", 2020, 325m),
            ("Volkswagen", "T-Cross", 2021, 625m),
            ("Volkswagen", "Tiguan", 2022, 900m),
            ("Volkswagen", "Amarok", 2023, 1050m),
            ("Suzuki", "Swift", 2024, 325m),
            ("Suzuki", "Baleno", 2025, 375m),
            ("Suzuki", "Fronx", 2023, 475m),
            ("Suzuki", "Jimny", 2021, 700m),
            ("Suzuki", "Ertiga", 2022, 600m),
            ("Hyundai", "Grand i10", 2023, 300m),
            ("Hyundai", "i20", 2024, 400m),
            ("Hyundai", "Venue", 2025, 525m),
            ("Hyundai", "Creta", 2020, 650m),
            ("Hyundai", "Tucson", 2021, 850m),
            ("Kia", "Picanto", 2022, 300m),
            ("Kia", "Rio", 2023, 400m),
            ("Kia", "Sonet", 2024, 525m),
            ("Kia", "Seltos", 2025, 675m),
            ("Kia", "Sportage", 2020, 900m),
            ("Ford", "Figo", 2021, 350m),
            ("Ford", "EcoSport", 2022, 550m),
            ("Ford", "Ranger", 2023, 950m),
            ("Ford", "Everest", 2024, 1150m),
            ("Nissan", "Micra", 2025, 350m),
            ("Nissan", "Magnite", 2020, 450m),
            ("Nissan", "Qashqai", 2021, 750m),
            ("Nissan", "Navara", 2022, 925m),
            ("Renault", "Kwid", 2023, 275m),
            ("Renault", "Clio", 2024, 400m),
            ("Renault", "Duster", 2025, 625m),
            ("Renault", "Triber", 2020, 475m),
            ("Haval", "Jolion", 2021, 650m),
            ("Haval", "H6", 2022, 825m),
            ("Chery", "Tiggo 4 Pro", 2023, 600m),
            ("Chery", "Tiggo 7 Pro", 2024, 775m),
            ("Mazda", "Mazda2", 2025, 400m),
            ("Mazda", "CX-3", 2020, 625m),
            ("Mazda", "CX-5", 2021, 850m),
            ("Honda", "Amaze", 2022, 375m),
            ("Honda", "Fit", 2023, 475m),
            ("Honda", "HR-V", 2024, 750m),
            ("Isuzu", "D-Max", 2025, 950m),
            ("BMW", "320i", 2020, 1250m),
            ("Mercedes-Benz", "C200", 2021, 1350m)
        ];

        var added = 0;
        for (var index = 0; index < fleet.Length; index++)
        {
            var registration = new RegistrationNumber($"DEMO{index + 1:000} GP");
            if (!existing.Add(registration.Value)) continue;

            var item = fleet[index];
            database.Vehicles.Add(Vehicle.Register(
                registration,
                item.Make,
                item.Model,
                new VehicleYear(item.Year),
                new DailyRate(item.Rate)));
            added++;
        }

        if (added > 0) await database.SaveChangesAsync(cancellationToken);
        return added;
    }
}
