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
        bool added = Add(reservationEntry);
        
        if (!added)
        {
            return BadRequest();
        }
        
        // Notify clients via SignalR
        await hubContext.Clients.All.ReservationAdded(reservationEntry);

        return Ok(reservationEntry);
    }

    private bool Add(ReservationEntry reservationEntry)
    {
        // Set a Redis Hashset entry where
        // <param name="key"> is the reservationEntry.Tags[0]</param>
        // <param name="hashField">is the reservations.id</param>
        // <param name="value">is the reservations.name</param>
        var key = reservationEntry.Device;
        var hashField = reservationEntry.Id.ToString();
        var value = reservationEntry.Name;
        
        bool res = _db.HashSetAsync(key, hashField, value).Result;
        Console.WriteLine(res); 
        return res;
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to delete the reservationEntry from a database if needed
        bool removed = Remove(reservationEntry);
        if (!removed)
        {
            return BadRequest();
        }
        // and remove it
     //   _reservationEntries.Remove(reservationEntry);
        var reservationId = reservationEntry.Id;
        // Notify clients via SignalR
        await hubContext.Clients.All.ReservationDeleted(reservationId);

        return Ok(reservationId);
    }
    
    private bool Remove(ReservationEntry reservationEntry)
    {
        var key = reservationEntry.Device;
        var value = reservationEntry.Id;
        bool res = _db.HashDelete(key, value);
        Console.WriteLine(res); 
        return res;
    }
}