using System.Collections.Concurrent;
using CoreEvents.Models.Domain;

namespace CoreEvents.Data.Repositories
{
    public class InMemoryBookingRepository : IBookingRepository
    {
        private readonly ConcurrentDictionary<Guid, Booking> _dictionary = new();

        public IEnumerable<Booking> GetAll(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return _dictionary.Values;
        }

        public Booking? GetById(Guid id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return _dictionary.GetValueOrDefault(id);
        }

        public void Add(Booking entity, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
            _dictionary.TryAdd(entity.Id, entity);
        }

        public void Update(Booking entity, CancellationToken ct)
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

        public IEnumerable<Booking> GetPending(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return _dictionary.Select(x => x.Value)
                .Where(x => x.Status == BookingStatus.Pending); ;
        }
    }
}
