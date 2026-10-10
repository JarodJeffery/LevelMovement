using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentValidation;
using LevelMovement.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace LevelMovement.Api.Middleware;
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ValidationException => 400,
            KeyNotFoundException => 404,
            DbUpdateConcurrencyException or VehicleUnavailableException => 409,
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => 409,
            _ => 500
        };
        var endpoint = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "Unknown endpoint";
        var method = GetFailingFunction(exception, endpoint);
        logger.Log(status == 500 ? LogLevel.Error : LogLevel.Warning, exception,
            "Request failed. TraceId {TraceId} Endpoint {Endpoint} CqrsRequest {CqrsRequest} FailingFunction {FailingFunction} StatusCode {StatusCode}",
            context.TraceIdentifier, endpoint, context.Items["CqrsRequest"] ?? "ModelBinding", method, status);
        var extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier };
        if (exception is ValidationException validation)
        {
            var errors = validation.Errors.GroupBy(error => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            await Results.ValidationProblem(errors, extensions: extensions).ExecuteAsync(context);
            return true;
        }
        var (title, detail) = exception switch
        {
            KeyNotFoundException => ("Vehicle was not found.", "No vehicle is available for this operation."),
            DbUpdateConcurrencyException => ("Vehicle has changed.", "Reload the vehicle and use its latest concurrency stamp before updating or deleting."),
            VehicleUnavailableException => ("Vehicle cannot be booked.", "Deleted vehicles cannot be booked."),
            DbUpdateException when status == 409 => ("Registration number already exists.", "Use a different registration number. Deleted vehicles also retain their registration."),
            _ => ("An unexpected error occurred.", "Use the trace ID when reporting this error.")
        };
        await Results.Problem(statusCode: status, title: title, detail: detail, extensions: extensions).ExecuteAsync(context);
        return true;
    }

    public static string GetFailingFunction(Exception exception, string fallback)
    {
        foreach (var frame in new StackTrace(exception, false).GetFrames())
        {
            var method = frame.GetMethod();
            var type = method?.DeclaringType;
            if (type?.Assembly.GetName().Name?.StartsWith("LevelMovement", StringComparison.Ordinal) != true) continue;
            if (method!.Name == "MoveNext" && type.DeclaringType is { } parent)
            {
                var original = parent.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(candidate => candidate.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType == type);
                if (original is not null) return $"{parent.FullName}.{original.Name}";
            }
            return $"{type.FullName}.{method.Name}";
        }
        return fallback;
    }
}
