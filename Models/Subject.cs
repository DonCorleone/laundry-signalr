using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LaundrySignalR.Models;

public class Subject
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    
    // Multi-tenant support
    public string TenantId { get; set; } = string.Empty;
    
    // MongoDB metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}