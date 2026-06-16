using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LaundrySignalR.Models;

public class Tenant
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // Unique tenant identifier
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}