namespace LaundrySignalR.Models;

/// <summary>
/// Response model for reservation data sent to frontend
/// Hides MongoDB ObjectId complexity and uses meaningful composite IDs
/// </summary>
public class ReservationResponse
{
    /// <summary>
    /// Unique identifier combining date and device (e.g., "2025-09-28T14:00:00-WM1")
    /// This is the ConnectionId from the internal model, exposed as a simple ID
    /// </summary>
    /// <example>2025-09-28T14:00:00-WM1</example>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the person who made the reservation
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Device identifier (washing machine, dryer, etc.)
    /// </summary>
    /// <example>WM1</example>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Reservation date and time in ISO format
    /// </summary>
    /// <example>2025-09-28T14:00:00.000Z</example>
    public string Date { get; set; } = string.Empty;
    
    /// <summary>
    /// Creates a ReservationResponse from a ReservationEntry
    /// </summary>
    public static ReservationResponse FromReservationEntry(ReservationEntry reservation)
    {
        return new ReservationResponse
        {
            Id = reservation.ConnectionId, // ConnectionId becomes the frontend ID
            Name = reservation.Name,
            DeviceId = reservation.DeviceId,
            Date = reservation.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }
}