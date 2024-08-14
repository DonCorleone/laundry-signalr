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
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to save the reservationEntry to a database if needed

        // Notify clients via SignalR
        await hubContext.Clients.All.ReservationAdded(reservationEntry);

        return Ok(reservationEntry);
    }
    
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] int reservationId)
    {
        // Here you can add code to save the reservationEntry to a database if needed

        // Notify clients via SignalR
        await hubContext.Clients.All.ReservationDeleted(reservationId);

        return Ok(reservationId);
    }
}