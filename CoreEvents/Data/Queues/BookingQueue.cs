using System.Threading.Channels;
using CoreEvents.Models.Domain;

namespace CoreEvents.Data.Queues
{
    public class BookingQueue: IBookingQueue, IDisposable
    {
        private readonly Channel<Booking> _channel;
        private readonly ChannelWriter<Booking> _writer;
        private readonly ChannelReader<Booking> _reader;
        public BookingQueue()
        {
            var options = new UnboundedChannelOptions()
            {
                SingleReader = false,
                SingleWriter = false
            };
            _channel = Channel.CreateUnbounded<Booking>(options);
            _writer = _channel.Writer;
            _reader = _channel.Reader;

        }
        public ValueTask EnqueueAsync(Booking bookGuid, CancellationToken ct)
        {
            return _writer.WriteAsync(bookGuid, ct);
        }

        public IAsyncEnumerable<Booking> DequeueAsync(CancellationToken ct)
        {
            return _reader.ReadAllAsync(ct);
        }

        public void Dispose()
        {
            _writer.Complete();
        }
    }
}
