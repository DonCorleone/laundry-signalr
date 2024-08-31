using LaundrySignalR.Models;
using LaundrySignalR.Services;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Hubs;

public class ReservationHub(ILogger<ReservationHub> logger, IRedisService redisService) : Hub<IReservationHubClients>
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {0}", Context.ConnectionId);

        // Read machineIds from query string
        var query = Context.GetHttpContext()?.Request.Query;
        var machineIds = query?.Where(q => q.Key.StartsWith("machineid"))
            .Select(q => q.Value.ToString())
            .ToArray();

        if (machineIds == null || machineIds.Length == 0)
        {
            logger.LogWarning("Machine IDs are missing in the query string.");
            await base.OnConnectedAsync();
            return;
        }

        var db = redisService.GetDatabase();
        var reservationEntries = new List<ReservationEntry>();

        foreach (var machineId in machineIds) {
            // load all reservations from the database
            var hashEntries = db.HashGetAll(machineId);

            // map the reservations to ReservationEntry objects
            var entries = hashEntries.Select(entry => new ReservationEntry
            {
                Id = entry.Name,
                Name = entry.Value.HasValue ? entry.Value.ToString() : string.Empty,
                DeviceId = machineId
            });

            reservationEntries.AddRange(entries);
        }

        await Clients.Caller.ReservationsLoaded(reservationEntries);
        await base.OnConnectedAsync();
    }
}