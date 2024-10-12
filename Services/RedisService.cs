// File: Services/RedisService.cs

using LaundrySignalR.Models;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace LaundrySignalR.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _db;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1); // Set your default expiration time here

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
                Date = entry.Name.ToString().Substring(0, 24)
            });

            reservationEntries.AddRange(entries);
        }

        return reservationEntries;
    }

    public async Task<bool> Add(ReservationEntry reservationEntry)
    {
        var key = reservationEntry.DeviceId;
        var hashField = reservationEntry.Id;
        var value = reservationEntry.Name;

        // Parse the Date property to get the expiration time in milliseconds
        if (!DateTimeOffset.TryParse(reservationEntry.Date, out var dateTimeOffset))
        {
            throw new ArgumentException("Invalid date format in reservation entry.");
        }
        var expirationTime = dateTimeOffset.ToUnixTimeMilliseconds();

        var hashSetResult = await _db.HashSetAsync(key, hashField, value);

        // Fire and forget SortedSetAddAsync
        Task.Run(() => _db.SortedSetAddAsync("reservations_expirations", hashField, expirationTime));

        return hashSetResult;
    }

    public async Task<bool> Remove(ReservationEntry reservationEntry)
    {
        var key = reservationEntry.DeviceId;
        var value = reservationEntry.Id;

        var hashDeleteResult = await _db.HashDeleteAsync(key, value);
        var sortedSetRemoveResult = await _db.SortedSetRemoveAsync("reservations_expirations", value);

        return hashDeleteResult && sortedSetRemoveResult;
    }
}