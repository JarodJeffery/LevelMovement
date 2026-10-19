using LevelMovement.Application.Vehicles.Queries;
using LevelMovement.Application.Vehicles.Commands;
using MediatR;
using LevelMovement.Application.Vehicles.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace LevelMovement.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class VehiclesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<UpSertVehicleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<UpSertVehicleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpSertVehicleResponse>> Upsert(
        [FromBody] UpsertVehicleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return StatusCode(
            result.IsCreated ? StatusCodes.Status201Created : StatusCodes.Status200OK,
            result.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType<VehiclePageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VehiclePageResponse>> GetAll(
        CancellationToken cancellationToken, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        => Ok(await sender.Send(new GetVehiclesQuery(pageNumber, pageSize), cancellationToken));

    [HttpGet("{registrationNumber}")]
    [ProducesResponseType<VehicleDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VehicleDetailsResponse>> GetDetails(string registrationNumber, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetVehicleDetailsQuery(registrationNumber), cancellationToken));

    [HttpDelete("{registrationNumber}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(string registrationNumber, [FromBody] DeleteVehicleRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteVehicleCommand(registrationNumber, request.ConcurrencyStamp), cancellationToken);
        return NoContent();
    }
}
