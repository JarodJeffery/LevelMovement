using LevelMovement.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LevelMovement.Domain.ValueObjects.Vehicles;

namespace LevelMovement.Infrastructure.Persistence.Configurations
{
    public class VehicleConfig : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.ToTable("Vehicles");

            builder.HasKey(vehicle => vehicle.Id);

            builder.Property(vehicle => vehicle.RegistrationNumber)
                .HasConversion(number => number.Value, value => new RegistrationNumber(value))
                .IsRequired()
                .HasMaxLength(RegistrationNumber.MaxLength);

            builder.HasIndex(vehicle => vehicle.RegistrationNumber)
                .IsUnique();

            builder.Property(vehicle => vehicle.Make)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(vehicle => vehicle.Model)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(vehicle => vehicle.Year)
                .HasConversion(year => year.Value, value => new VehicleYear(value))
                .IsRequired();

            builder.Property(vehicle => vehicle.DailyRate)
                .HasConversion(rate => rate.Amount, value => new DailyRate(value))
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(vehicle => vehicle.IsDeleted)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(vehicle => vehicle.CreatedDate)
                .HasColumnType("datetime2")
                .IsRequired();

            builder.Property(vehicle => vehicle.ConcurrencyStamp)
                .IsConcurrencyToken()
                .ValueGeneratedNever()
                .IsRequired();

            builder.HasQueryFilter(vehicle => !vehicle.IsDeleted);
        }
    }
}
