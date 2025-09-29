using System.ComponentModel.DataAnnotations;

namespace LaundrySignalR.Models;

/// <summary>
/// Request model for creating or updating reservations
/// Simplified for frontend compatibility - hides MongoDB complexity
/// </summary>
public class CreateReservationRequest
{
    /// <summary>
    /// Name of the person making the reservation
    /// </summary>
    /// <example>John Doe</example>
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Device identifier (e.g., washing machine or dryer ID)
    /// </summary>
    /// <example>WM1</example>
    [Required(ErrorMessage = "DeviceId is required")]
    public string DeviceId { get; set; } = string.Empty; 
    
    /// <summary>
    /// Reservation date and time in ISO format
    /// </summary>
    /// <example>2025-09-28T14:00:00Z</example>
    [Required(ErrorMessage = "Date is required")]
    public string Date { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional connection ID for SignalR notifications
    /// </summary>
    /// <example>2025-09-28T14:00:00-WM1</example>
    public string? ConnectionId { get; set; }
    
    /// <summary>
    /// Optional ID to use as the reservation identifier (frontend tile ID)
    /// </summary>
    /// <example>2025-10-01T06:00:00.000Z-WM1</example>
    public string? Id { get; set; }
}