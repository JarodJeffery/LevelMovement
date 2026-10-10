namespace LevelMovement.Domain.Exceptions;
public sealed class VehicleUnavailableException : Exception
{
    public VehicleUnavailableException() : base("Deleted vehicles cannot be booked.") { }
}
