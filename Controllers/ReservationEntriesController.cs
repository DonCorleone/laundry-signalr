using System.Runtime.InteropServices.JavaScript;
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

    private readonly IDatabase _db = redisService.GetDatabase();

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var query = HttpContext.Request.Query;
        var machineIds = query.Where(q => q.Key.StartsWith("machineid"))
            .Select(q => q.Value.ToString())
            .ToArray();

        if (machineIds.Length == 0)
        {
            return BadRequest("Machine IDs are missing in the query string.");
        }

        var reservationEntries = new List<ReservationEntry>();

        foreach (var machineId in machineIds)
        {
            var hashEntries = await _db.HashGetAllAsync(machineId);
            var entries = hashEntries.Select(entry => new ReservationEntry
            {
                Id = entry.Name,
                Name = entry.Value.HasValue ? entry.Value.ToString() : string.Empty,
                DeviceId = machineId,
                Date = entry.Name.ToString().Substring(0,24)
            });

            reservationEntries.AddRange(entries);
        }

        return Ok(reservationEntries);
    }
    
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to save the reservationEntry to a database if needed
        try
        {
            var added = await Add(reservationEntry);
        
            // Notify clients via SignalR
            if (added)
            {
                await hubContext.Clients.All.ReservationAdded(reservationEntry);
            }else
            {
                await hubContext.Clients.All.ReservationUpdated(reservationEntry);
            }
            
            return Ok(reservationEntry);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    private Task<bool> Add(ReservationEntry reservationEntry)
    {
        var key = reservationEntry.DeviceId;
        var hashField = reservationEntry.Id;
        var value = reservationEntry.Name;
        
        var res = _db.HashSetAsync(key, hashField, value).Result;
        Console.WriteLine(res); 
        return Task.FromResult(res);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to delete the reservationEntry from a database if needed
        var removed = await Remove(reservationEntry);
        if (!removed)
        {
            return BadRequest();
        }

        var reservationId = reservationEntry.Id;
        // Notify clients via SignalR
        await hubContext.Clients.All.ReservationDeleted(reservationId);

        return Ok(new {reservationId});
    }
    
    private Task<bool> Remove(ReservationEntry reservationEntry)
    {
        var key = reservationEntry.DeviceId;
        var value = reservationEntry.Id;
        var res = _db.HashDeleteAsync(key, value);
        Console.WriteLine(res); 
        return res;
    }
}