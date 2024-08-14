using LaundrySignalR.Models;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Hubs;

public class ReservationHub(ILogger<ReservationHub> logger) : Hub<IReservationHubClients>
{
}