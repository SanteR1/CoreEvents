using CoreEvents.Models.Domain;

namespace CoreEvents.Data.Repositories
{
    public class InMemoryRepository<T> : IRepository<T> where T : IEntity
    {
        private readonly List<T> _entities = new();

        public IEnumerable<T> GetAll() => _entities;

        public T? GetById(Guid id) => _entities.FirstOrDefault(e => e.Id == id);

        public void Add(T entity)
        {
            if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
            _entities.Add(entity);
        }

        public void Update(T entity)
        {
            var existing = GetById(entity.Id);
            if (existing != null)
            {
                var index = _entities.IndexOf(existing);
                _entities[index] = entity;
            }
        }

        public void Delete(Guid id)
        {
            var entity = GetById(id);
            if (entity != null) _entities.Remove(entity);
        }
    }
}
