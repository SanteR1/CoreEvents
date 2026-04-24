namespace CoreEvents.Data.Queues
{
    public interface IQueueSource<TKey>
    {
        void Enqueue(TKey id);
        bool TryDequeue(out TKey id);
    }
}
