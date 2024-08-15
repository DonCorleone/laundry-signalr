using LaundrySignalR.Hubs;
using LaundrySignalR.Models;
using LaundrySignalR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace LaundrySignalR.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReservationEntriesController : ControllerBase
{
// constructor
    public ReservationEntriesController(
        IHubContext<ReservationHub, IReservationHubClients> hubContext,
        IConfiguration configuration, IRedisService redisService)
    {
        _hubContext = hubContext;
        _redisService = redisService;
        
        // Access the configuration

        var redisPw = configuration["REDIS_CONNECTION_PWD"];
        Console.WriteLine(string.IsNullOrEmpty(redisPw)
            ? "Environment variable not found."
            : $"Environment variable value: {redisPw}");

        var redisUser = configuration["REDIS_CONNECTION_USER"];
        Console.WriteLine(string.IsNullOrEmpty(redisUser)
            ? "Environment variable not found."
            : $"Environment variable value: {redisUser}");

        var options = new StackExchange.Redis.ConfigurationOptions
        {
            EndPoints = { "frankfurt-redis.render.com:6379" },
            // use the password from render.com environment variables
            Password = redisPw,
            User = redisUser,
            Ssl = true,
            AbortOnConnectFail = false,
        };
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(options);
        IDatabase db = redis.GetDatabase();

        /*db.StringSet("foo", "bar");
        // Read all keys
        var server = redis.GetServer("frankfurt-redis.render.com:6379");
        var keys = server.Keys();

        // Read all values
        foreach (var key in keys)
        {
            var value = db.StringGet(key);
            Console.WriteLine($"Key: {key}, Value: {value}");
        }*/
        
        bool res1 = db.SortedSetAdd("racer_scores", "Norem", 10);
        Console.WriteLine(res1); // >>> True

        bool res2 = db.SortedSetAdd("racer_scores", "Castilla", 12);
        Console.WriteLine(res2); // >>> True

        long res3 = db.SortedSetAdd("racer_scores", new[]{
            new SortedSetEntry("Sam-Bodden", 8),
            new SortedSetEntry("Royce", 10),
            new SortedSetEntry("Ford", 6),
            new SortedSetEntry("Prickett", 14),
            new SortedSetEntry("Castilla", 12)
        });
        Console.WriteLine(res3); // >>> 4
        
        RedisValue[] res4 = db.SortedSetRangeByRank("racer_scores", 0, -1);
        Console.WriteLine(string.Join(", ", res4)); // >>> Ford, Sam-Bodden, Norem, Royce, Castilla, Prickett

        RedisValue[] res5 = db.SortedSetRangeByRank("racer_scores", 0, -1, Order.Descending);
        Console.WriteLine(string.Join(", ", res5)); // >>> Prickett, Castilla, Royce, Norem, Sam-Bodden, Ford


    }

    private readonly List<ReservationEntry> _reservationEntries = [];
    private readonly IHubContext<ReservationHub, IReservationHubClients> _hubContext;
    private readonly IRedisService _redisService;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to save the reservationEntry to a database if needed
        _reservationEntries.Add(reservationEntry);
        // Notify clients via SignalR
        await _hubContext.Clients.All.ReservationAdded(reservationEntry);

        return Ok(reservationEntry);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] long reservationId)
    {
        // Here you can add code to save the reservationEntry to a database if needed
        // Find the reservationEntry by Id
        var reservationEntry = _reservationEntries.FirstOrDefault(p => p.Id == reservationId);
        if (reservationEntry == null)
        {
            return NotFound();
        }

        // and remove it
        _reservationEntries.Remove(reservationEntry);

        // Notify clients via SignalR
        await _hubContext.Clients.All.ReservationDeleted(reservationId);

        return Ok(reservationId);
    }
}