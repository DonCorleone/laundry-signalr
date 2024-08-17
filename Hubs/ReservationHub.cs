using LaundrySignalR.Models;
using LaundrySignalR.Services;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Hubs;

public class ReservationHub(ILogger<ReservationHub> logger, IRedisService redisService) : Hub<IReservationHubClients>
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {0}", Context.ConnectionId);
        
        // Read machineId from query string
        var machineId = Context.GetHttpContext()?.Request.Query["machineId"].ToString();

        if (string.IsNullOrEmpty(machineId))
        {
            logger.LogWarning("Machine ID is missing in the query string.");
            await base.OnConnectedAsync();
            return;
        }
        // send all reservations to the client
        var db = redisService.GetDatabase();
            
        // load all reservations from the database
        var sortedSetEntries = db.SortedSetRangeByRankWithScores("Machine 1", 0, -1);
        
        // map the reservations to ReservationEntry objects
        var reservationEntries = sortedSetEntries.Select(entry => new ReservationEntry
        {
            Id = (long)entry.Score,
            Name = entry.Element,
            Timestamp = DateTime.Now,
            Tags = [machineId]
        });
        await Clients.Caller.ReservationsLoaded(reservationEntries);
        await base.OnConnectedAsync();
    }
}