using CoreEvents.Domain.Entities;

namespace CoreEvents.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);
        void Add(User entity);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
