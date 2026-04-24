using CoreEvents.Models.Domain;

namespace CoreEvents.Data.Repositories
{
    public interface IRepository<T> where T : IEntity
    {
        IEnumerable<T> GetAll(CancellationToken ct = default);
        T? GetById(Guid id, CancellationToken ct = default);
        void Add(T entity, CancellationToken ct = default);
        void Update(T entity, CancellationToken ct = default);
        void Delete(Guid id, CancellationToken ct = default);
    }
}
