using StackExchange.Redis;

namespace LaundrySignalR.Services;

public interface IRedisService
{
    IDatabase GetDatabase();
}