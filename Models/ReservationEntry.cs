using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LaundrySignalR.Models;

public class ReservationEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    
    // Multi-tenant support
    public string TenantId { get; set; } = string.Empty;
    
    // MongoDB metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Index for expiration (TTL)
    public DateTime ExpiresAt { get; set; }
}