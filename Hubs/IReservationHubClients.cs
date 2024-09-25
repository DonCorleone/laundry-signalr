using LaundrySignalR.Models;

namespace LaundrySignalR.Hubs;

public interface IReservationHubClients
{ 
    Task ReservationsLoaded(IEnumerable<ReservationEntry> reservationEntries);
    Task ReservationAdded(ReservationEntry reservationEntry);
    Task ReservationDeleted(string reservationEntry);
    Task ReservationUpdated(ReservationEntry reservationEntry);
}