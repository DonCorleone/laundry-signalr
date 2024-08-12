using LaundrySignalR.Models;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Hubs;

public class ReservationHub(ILogger<ReservationHub> logger) : Hub
{
    public async Task NewMessage(ReservationEntry message)
    {
        try
        {
            logger.LogInformation("New message from {name}: {date}", message.Name, message.Timestamp);
            await Clients.All.SendAsync("messageReceived", message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in NewMessage");
            throw;
        }
    }
}