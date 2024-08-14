using LaundrySignalR.Hubs;
using LaundrySignalR.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReservationEntriesController : ControllerBase
{
    private readonly IHubContext<ReservationHub, IReservationHubClients> _hubContext;

    public ReservationEntriesController(IHubContext<ReservationHub, IReservationHubClients> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReservationEntry reservationEntry)
    {
        // Here you can add code to save the reservationEntry to a database if needed

        // Notify clients via SignalR
        await _hubContext.Clients.All.ReservationAdded(reservationEntry);

        return Ok(reservationEntry);
    }
}