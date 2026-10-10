using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LevelMovement.Api.Middleware;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationException validation)
        {
            var errors = validation.Errors
                .GroupBy(error => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            await Results.ValidationProblem(errors).ExecuteAsync(context);
            return true;
        }

        if (exception is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } })
        {
            await Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Registration number already exists.",
                detail: "Use a different registration number. Deleted vehicles also retain their registration.")
                .ExecuteAsync(context);
            return true;
        }

        return false;
    }
}
