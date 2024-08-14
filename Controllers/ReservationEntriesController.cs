using LaundrySignalR.Hubs;
using LaundrySignalR.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReservationEntriesController(IHubContext<ReservationHub, IReservationHubClients> hubContext)
    : ControllerBase
{
    private readonly List<ReservationEntry> _reservationEntries = [];
    
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to save the reservationEntry to a database if needed
        _reservationEntries.Add(reservationEntry);
        // Notify clients via SignalR
        await hubContext.Clients.All.ReservationAdded(reservationEntry);

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
        await hubContext.Clients.All.ReservationDeleted(reservationId);

        return Ok(reservationId);
    }
}