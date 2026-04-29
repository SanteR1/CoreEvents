using System.Collections.Concurrent;

namespace CoreEvents.Data.Queues
{
    public class InMemoryBookingQueue : IQueueSource<Guid>
    {
        private readonly ConcurrentQueue<Guid> _queue = new();
        public void Enqueue(Guid id)
        {
            _queue.Enqueue(id);
        }

        public bool TryDequeue(out Guid id)
        {
            return _queue.TryDequeue(out id);
        }
    }
}
