using LevelMovement.Application.Vehicles.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LevelMovement.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateVehicleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateVehicleResponse>> Create(
        [FromBody] CreateVehicleCommand command,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new CreateVehicleResponse(id));
    }
}

public sealed record CreateVehicleResponse(Guid Id);
