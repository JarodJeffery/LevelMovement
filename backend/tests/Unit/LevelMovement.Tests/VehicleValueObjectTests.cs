using System.Globalization;
using LevelMovement.Domain.ValueObjects.Vehicles;
using LevelMovement.Domain.Vehicles;
using LevelMovement.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LevelMovement.Tests;

public class VehicleValueObjectTests
{
    [Theory]
    [InlineData("10.125", "10.12")]
    [InlineData("10.135", "10.14")]
    [InlineData("10.1249", "10.12")]
    [InlineData("10.1251", "10.13")]
    [InlineData("0", "0")]
    [InlineData("-10.125", "-10.12")]
    public void DailyRateRoundsToEvenWithoutRestrictingSign(string input, string expected)
    {
        var rate = new DailyRate(decimal.Parse(input, CultureInfo.InvariantCulture));

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), rate.Amount);
    }

    [Fact]
    public void ValueObjectsCompareByTheirNormalizedValues()
    {
        Assert.Equal(new DailyRate(10.125m), new DailyRate(10.12m));
        Assert.Equal(new RegistrationNumber(" ab-123 gp "), new RegistrationNumber("AB-123 GP"));
        Assert.Equal(new VehicleYear(2026), new VehicleYear(2026));
        Assert.NotEqual(new VehicleYear(2025), new VehicleYear(2026));
        Assert.Equal(new DailyRate(10.12m).GetHashCode(), new DailyRate(10.125m).GetHashCode());
    }

    [Fact]
    public void RegistrationPreservesInternalSpacesAndPunctuation()
    {
        Assert.Equal("AB-123  GP", new RegistrationNumber("  ab-123  gp  ").Value);
        Assert.Equal(new string('A', 20), new RegistrationNumber(" " + new string('a', 20) + " ").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public void RegistrationRejectsInvalidInput(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new RegistrationNumber(value!));
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(9999)]
    [InlineData(2026)]
    public void YearAcceptsFourDigits(int value)
    {
        Assert.Equal(value, new VehicleYear(value).Value);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(10000)]
    [InlineData(-1)]
    public void YearRejectsValuesOutsideTheRange(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VehicleYear(value));
    }

    [Fact]
    public void SqlServerMappingKeepsScalarColumnsAndRoundTripsValues()
    {
        using var context = new MappingContext();
        var entity = context.Model.FindEntityType(typeof(Vehicle))!;
        var samples = new (string Name, object Value, object Stored)[]
        {
            (nameof(Vehicle.RegistrationNumber), new RegistrationNumber(" ab-123 "), "AB-123"),
            (nameof(Vehicle.Year), new VehicleYear(2026), 2026),
            (nameof(Vehicle.DailyRate), new DailyRate(10.125m), 10.12m)
        };

        foreach (var (name, value, stored) in samples)
        {
            var property = entity.FindProperty(name)!;
            var converter = property.GetTypeMapping().Converter!;
            Assert.Equal(stored, converter.ConvertToProvider(value));
            Assert.Equal(value, converter.ConvertFromProvider(stored));
            Assert.False(property.IsNullable);
        }

        Assert.Single(context.Model.GetEntityTypes());
        Assert.Equal(20, entity.FindProperty(nameof(Vehicle.RegistrationNumber))!.GetMaxLength());
        Assert.True(entity.GetIndexes().Single().IsUnique);
        Assert.True(entity.FindProperty(nameof(Vehicle.ConcurrencyStamp))!.IsConcurrencyToken);
        var sql = context.Database.GenerateCreateScript();
        Assert.Contains("[DailyRate] decimal(18,2) NOT NULL", sql);
        Assert.Contains("[Year] int NOT NULL", sql);
        Assert.Contains("[RegistrationNumber] nvarchar(20) NOT NULL", sql);
    }

    private sealed class MappingContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=MappingOnly;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new VehicleConfig());
        }
    }
}
