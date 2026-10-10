using MediatR;

namespace LevelMovement.Application.Interfaces.Persistence;

public interface ICommand<TResponse> : IRequest<TResponse>;
