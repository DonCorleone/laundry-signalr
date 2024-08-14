using LaundrySignalR.Models;

namespace LaundrySignalR.Hubs;

public interface IReservationHubClients
{ 
    Task ReservationAdded(ReservationEntry reservationEntry);
    Task ReservationDeleted(int reservationEntry);
}