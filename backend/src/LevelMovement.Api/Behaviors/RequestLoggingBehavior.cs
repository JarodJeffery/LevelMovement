using MediatR;
namespace LevelMovement.Api.Behaviors;
public sealed class RequestLoggingBehavior<TRequest, TResponse>(IHttpContextAccessor accessor) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (accessor.HttpContext is { } context) context.Items["CqrsRequest"] = typeof(TRequest).Name;
        return next();
    }
}
