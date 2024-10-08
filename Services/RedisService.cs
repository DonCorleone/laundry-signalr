// File: Services/RedisService.cs

using LaundrySignalR.Models;
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
    
    public async Task<List<ReservationEntry>> GetAllEntries(List<Subject> subjects)
    {
        var reservationEntries = new List<ReservationEntry>();
        foreach (var subject in subjects)
        {
            var hashEntries = await _db.HashGetAllAsync(subject.Key);
            var entries = hashEntries.Select(entry => new ReservationEntry
            {
                Id = entry.Name,
                Name = entry.Value.HasValue ? entry.Value.ToString() : string.Empty,
                DeviceId = subject.Key,
                Date = entry.Name.ToString().Substring(0,24)
            });

            reservationEntries.AddRange(entries);
        }

        return reservationEntries;
    }
    public Task<bool> Add(ReservationEntry reservationEntry)
    {
        var key = reservationEntry.DeviceId;
        var hashField = reservationEntry.Id;
        var value = reservationEntry.Name;
        
        var res = _db.HashSetAsync(key, hashField, value).Result;
        
        Console.WriteLine(res); 
        return Task.FromResult(res);
    }
    
    public Task<bool> Remove(ReservationEntry reservationEntry)
    {
        var key = reservationEntry.DeviceId;
        var value = reservationEntry.Id;
        var res = _db.HashDeleteAsync(key, value);
        Console.WriteLine(res); 
        return res;
    }
}