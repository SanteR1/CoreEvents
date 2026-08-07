using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Domain.Entities;
using CoreEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreEvents.Infrastructure.Repositories
{
    internal sealed class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Users.FindAsync([id], cancellationToken: ct);
        }

        public async Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.UserName == userName, ct);
        }

        public void Add(User entity)
        {
            _context.Users.Add(entity);
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }
    }
}
