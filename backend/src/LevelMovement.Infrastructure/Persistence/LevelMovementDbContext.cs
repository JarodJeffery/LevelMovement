using LevelMovement.Domain.Vehicles;
using LevelMovement.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LevelMovement.Infrastructure.Persistence
{
    public class LevelMovementDbContext : DbContext
    {
        public LevelMovementDbContext(
            DbContextOptions<LevelMovementDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles => Set<Vehicle>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new VehicleConfig());
        }
    }
}
