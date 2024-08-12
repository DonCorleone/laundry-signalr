using LaundrySignalR.Hubs;
using LaundrySignalR.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReservationEntriesController : ControllerBase
{
    private readonly IHubContext<ReservationHub> _hubContext;

    public ReservationEntriesController(IHubContext<ReservationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReservationEntry reservationEntry)
    {
        if (reservationEntry == null) return BadRequest();

        // Here you can add code to save the reservationEntry to a database if needed

        // Notify clients via SignalR
        await _hubContext.Clients.All.SendAsync("newMessage", reservationEntry);

        return Ok(reservationEntry);
    }
}