using LaundrySignalR.Models;

namespace LaundrySignalR.Hubs;

public interface IReservationHubClients
{ 
    Task ReservationsLoaded(IEnumerable<ReservationResponse> reservations);
    Task ReservationAdded(ReservationResponse reservation);
    Task ReservationDeleted(string reservationId); // This is the ConnectionId
    Task ReservationUpdated(ReservationResponse reservation);
}