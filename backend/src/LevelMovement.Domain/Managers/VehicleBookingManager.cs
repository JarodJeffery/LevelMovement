using LevelMovement.Domain.Exceptions;
using LevelMovement.Domain.Vehicles;
namespace LevelMovement.Domain.Managers;
public sealed class VehicleBookingManager
{
    public void EnsureEligible(Vehicle vehicle)
    {
        if (vehicle.IsDeleted) throw new VehicleUnavailableException();
    }
}
