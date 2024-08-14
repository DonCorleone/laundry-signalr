using LaundrySignalR.Models;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Hubs;

public class ReservationHub(ILogger<ReservationHub> logger) : Hub<IReservationHubClients>
{
    public async Task ReservationAdded(ReservationEntry reservationEntry)
    {
        try
        {
            logger.LogInformation("New reservation from {name}: {date}", reservationEntry.Name, reservationEntry.Timestamp);
            await Clients.All.ReservationAdded(reservationEntry);        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in NewMessage");
            throw;
        }
    }
}