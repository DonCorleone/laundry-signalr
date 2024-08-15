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
        bool res = _db.SortedSetAdd(reservationEntry.Tags[0], reservationEntry.Name, reservationEntry.Id);
        Console.WriteLine(res); 
        return res;
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