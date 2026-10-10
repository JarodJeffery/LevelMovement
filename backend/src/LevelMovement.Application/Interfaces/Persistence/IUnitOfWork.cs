using System;
using System.Collections.Generic;
using System.Text;

namespace LevelMovement.Application.Interfaces.Persistence
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken);
    }
}
