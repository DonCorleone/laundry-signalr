using LaundrySignalR.Models;
using LaundrySignalR.Services;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Hubs;

public class ReservationHub(ILogger<ReservationHub> logger, IRedisService redisService, IJsonFileService jsonFileService) : Hub<IReservationHubClients>
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {0}", Context.ConnectionId);

        var subjects = await jsonFileService.LoadSubjects();

        if (subjects == null || subjects.Count == 0)
        {
            logger.LogWarning("Machine IDs are missing in the query string.");
            await base.OnConnectedAsync();
            return;
        }

        var db = redisService.GetDatabase();
        var reservationEntries = new List<ReservationEntry>();

        foreach (var subject in subjects) {
            // load all reservations from the database
            var hashEntries = db.HashGetAll(subject.Key);

            // map the reservations to ReservationEntry objects
            var entries = hashEntries.Select(entry => new ReservationEntry
            {
                Id = entry.Name,
                Name = entry.Value.HasValue ? entry.Value.ToString() : string.Empty,
                DeviceId = subject.Key
            });

            reservationEntries.AddRange(entries);
        }

        await Clients.Caller.ReservationsLoaded(reservationEntries);
        await base.OnConnectedAsync();
    }
}