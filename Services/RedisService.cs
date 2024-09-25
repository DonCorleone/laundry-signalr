// File: Services/RedisService.cs
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
namespace LaundrySignalR.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _db;

    public RedisService(ConfigurationOptions options)
    {
        var redis = ConnectionMultiplexer.Connect(options);
        _db = redis.GetDatabase();
    }

    public IDatabase GetDatabase() => _db;
}