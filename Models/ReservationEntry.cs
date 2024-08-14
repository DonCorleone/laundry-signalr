namespace LaundrySignalR.Models;

public class ReservationEntry
{
    public long Id { get; set; }
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> Tags { get; set; }
}