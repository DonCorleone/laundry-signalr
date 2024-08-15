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
public class ReservationEntriesController(
    IHubContext<ReservationHub, IReservationHubClients> hubContext,
    IRedisService redisService)
    : ControllerBase
{
// constructor

    private readonly IDatabase _db = redisService.GetDatabase();

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to save the reservationEntry to a database if needed
        Add(reservationEntry);
        // Notify clients via SignalR
        await hubContext.Clients.All.ReservationAdded(reservationEntry);

        return Ok(reservationEntry);
    }

    private void Add(ReservationEntry reservationEntry)
    {
        bool res1 = _db.SortedSetAdd("racer_scores", "Norem", 10);
        Console.WriteLine(res1); // >>> True

        bool res2 = _db.SortedSetAdd("racer_scores", "Castilla", 12);
        Console.WriteLine(res2); // >>> True

        long res3 = _db.SortedSetAdd("racer_scores", new[]{
            new SortedSetEntry("Sam-Bodden", 8),
            new SortedSetEntry("Royce", 10),
            new SortedSetEntry("Ford", 6),
            new SortedSetEntry("Prickett", 14),
            new SortedSetEntry("Castilla", 12)
        });
        Console.WriteLine(res3); // >>> 4
        
        RedisValue[] res4 = _db.SortedSetRangeByRank("racer_scores", 0, -1);
        Console.WriteLine(string.Join(", ", res4)); // >>> Ford, Sam-Bodden, Norem, Royce, Castilla, Prickett

        RedisValue[] res5 = _db.SortedSetRangeByRank("racer_scores", 0, -1, Order.Descending);
        Console.WriteLine(string.Join(", ", res5)); // >>> Prickett, Castilla, Royce, Norem, Sam-Bodden, Ford

    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] long reservationId)
    {
        // Here you can add code to save the reservationEntry to a database if needed
        // Find the reservationEntry by Id
        /*var reservationEntry = _reservationEntries.FirstOrDefault(p => p.Id == reservationId);
        if (reservationEntry == null)
        {
            return NotFound();
        }*/

        // and remove it
     //   _reservationEntries.Remove(reservationEntry);

        // Notify clients via SignalR
        await hubContext.Clients.All.ReservationDeleted(reservationId);

        return Ok(reservationId);
    }
}