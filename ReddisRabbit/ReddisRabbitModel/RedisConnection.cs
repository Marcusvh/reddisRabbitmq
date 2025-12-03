using StackExchange.Redis;
namespace ReddisRabbitGameLogic.Services;

public class RedisConnection
{
    private readonly Lazy<ConnectionMultiplexer> _lazyConnection;

    public RedisConnection(string connectionString)
    {
        _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            ConnectionMultiplexer.Connect(connectionString));
    }

    public IDatabase Database => _lazyConnection.Value.GetDatabase();
    public ISubscriber Subscriber => _lazyConnection.Value.GetSubscriber();
    public ConnectionMultiplexer Connection => _lazyConnection.Value;
}
