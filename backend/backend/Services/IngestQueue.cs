using System.Threading.Channels;
using backend.DataStructure;

namespace backend.Services;

public class IngestQueue
{
    private readonly Channel<IngestTicket> _channel =
        Channel.CreateBounded<IngestTicket>(new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false
        });

    public ChannelReader<IngestTicket> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(IngestTicket ticket)
    {
        return _channel.Writer.WriteAsync(ticket);
    }
}