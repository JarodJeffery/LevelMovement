using LevelMovement.Application.Bookings.DTOs;
using LevelMovement.Application.Bookings.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace LevelMovement.Api.Controllers;
[ApiController]
[Route("api/bookings")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public sealed class BookingsController(ISender sender) : ControllerBase
{
    [HttpPost("validate")]
    [ProducesResponseType<BookingEligibilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BookingEligibilityResponse>> Validate([FromBody] ValidateBookingQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));
}
