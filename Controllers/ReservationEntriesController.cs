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
    IRedisService redisService, IJsonFileService jsonFileService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var subjects = await jsonFileService.LoadSubjects();
        if (subjects == null || subjects.Count == 0)
        {
            return BadRequest("No Subjects found");
        }

        var reservationEntries = await redisService.GetAllEntries(subjects);

        return Ok(reservationEntries);
    }
    

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to save the reservationEntry to a database if needed
        try
        {
            var added = await redisService.Add(reservationEntry);
        
            // Notify clients via SignalR
            if (added)
            {
                await hubContext.Clients.All
                    .ReservationAdded(reservationEntry);
            } else {
                await hubContext.Clients.All
                    .ReservationUpdated(reservationEntry);
            }
            
            return Ok(reservationEntry);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }



    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to delete the reservationEntry from a database if needed
        var removed = await redisService.Remove(reservationEntry);
        if (!removed)
        {
            return BadRequest();
        }

        var reservationId = reservationEntry.Id;
        // Notify clients via SignalR
        await hubContext.Clients.All.ReservationDeleted(reservationId);

        return Ok(new {reservationId});
    }
}