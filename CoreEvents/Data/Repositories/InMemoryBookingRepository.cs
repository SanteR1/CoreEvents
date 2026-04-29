using System.Collections.Concurrent;
using CoreEvents.Models.Domain;

namespace CoreEvents.Data.Repositories
{
    public class InMemoryBookingRepository<T> :  IRepository<T> where T : IEntity
    {
        private readonly ConcurrentDictionary<Guid, T> _dictionary = new();

        public IEnumerable<T> GetAll(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return _dictionary.Values;
        }

        public T? GetById(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return _dictionary.GetValueOrDefault(id);
        }

        public void Add(T entity, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
            _dictionary.TryAdd(entity.Id, entity);
        }

        public void Update(T entity, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var existing = GetById(entity.Id, ct);
            if (existing != null)
            {
                _dictionary[entity.Id] = entity;
            }
        }

        public void Delete(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var entity = GetById(id, ct);
            if (entity != null) _dictionary.TryRemove(entity.Id, out _);
        }
    }
}
