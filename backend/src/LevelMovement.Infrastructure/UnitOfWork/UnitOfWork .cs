using LevelMovement.Application.Interfaces.Persistence;
using LevelMovement.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace LevelMovement.Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LevelMovementDbContext _context;

        public UnitOfWork(LevelMovementDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
