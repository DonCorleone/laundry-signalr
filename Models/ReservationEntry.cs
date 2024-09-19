using System.Runtime.InteropServices.JavaScript;

namespace LaundrySignalR.Models;

public class ReservationEntry
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string DeviceId { get; set; }
    
    public string ConnectionId { get; set; }
    public string Date { get; set; }
}