using LaundrySignalR.Models;
using MongoDB.Driver;
using MongoDB.Bson;

namespace LaundrySignalR.Services;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Tenant> _tenants;
    private readonly IMongoCollection<Subject> _subjects;
    private readonly IMongoCollection<ReservationEntry> _reservations;
    private readonly ILogger<MongoDbService> _logger;

    public MongoDbService(IMongoDatabase database, ILogger<MongoDbService> logger)
    {
        _database = database;
        _logger = logger;
        _tenants = _database.GetCollection<Tenant>("tenants");
        _subjects = _database.GetCollection<Subject>("subjects");
        _reservations = _database.GetCollection<ReservationEntry>("reservations");
        
        // Create indexes for better performance
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        try
        {
            // Tenant indexes
            _tenants.Indexes.CreateOne(new CreateIndexModel<Tenant>(
                Builders<Tenant>.IndexKeys.Ascending(t => t.Code),
                new CreateIndexOptions { Unique = true }));

            // Subject indexes
            _subjects.Indexes.CreateOne(new CreateIndexModel<Subject>(
                Builders<Subject>.IndexKeys.Ascending(s => s.TenantId)));
            
            _subjects.Indexes.CreateOne(new CreateIndexModel<Subject>(
                Builders<Subject>.IndexKeys.Combine(
                    Builders<Subject>.IndexKeys.Ascending(s => s.TenantId),
                    Builders<Subject>.IndexKeys.Ascending(s => s.Key))));

            // Reservation indexes
            _reservations.Indexes.CreateOne(new CreateIndexModel<ReservationEntry>(
                Builders<ReservationEntry>.IndexKeys.Ascending(r => r.TenantId)));
                
            _reservations.Indexes.CreateOne(new CreateIndexModel<ReservationEntry>(
                Builders<ReservationEntry>.IndexKeys.Combine(
                    Builders<ReservationEntry>.IndexKeys.Ascending(r => r.TenantId),
                    Builders<ReservationEntry>.IndexKeys.Ascending(r => r.DeviceId))));

            // TTL index for automatic expiration
            _reservations.Indexes.CreateOne(new CreateIndexModel<ReservationEntry>(
                Builders<ReservationEntry>.IndexKeys.Ascending(r => r.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating MongoDB indexes");
        }
    }

    // Tenant management
    public async Task<Tenant?> GetTenantByCodeAsync(string tenantCode)
    {
        try
        {
            var filter = Builders<Tenant>.Filter.And(
                Builders<Tenant>.Filter.Eq(t => t.Code, tenantCode),
                Builders<Tenant>.Filter.Eq(t => t.IsActive, true));
                
            return await _tenants.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant by code: {TenantCode}", tenantCode);
            return null;
        }
    }

    public async Task<Tenant> CreateTenantAsync(Tenant tenant)
    {
        try
        {
            tenant.CreatedAt = DateTime.UtcNow;
            await _tenants.InsertOneAsync(tenant);
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant: {TenantCode}", tenant.Code);
            throw;
        }
    }

    // Subject management
    public async Task<List<Subject>> GetSubjectsAsync(string tenantId)
    {
        try
        {
            var filter = Builders<Subject>.Filter.And(
                Builders<Subject>.Filter.Eq(s => s.TenantId, tenantId),
                Builders<Subject>.Filter.Eq(s => s.IsActive, true));
                
            return await _subjects.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subjects for tenant: {TenantId}", tenantId);
            return new List<Subject>();
        }
    }

    public async Task<Subject> CreateSubjectAsync(Subject subject)
    {
        try
        {
            subject.CreatedAt = DateTime.UtcNow;
            await _subjects.InsertOneAsync(subject);
            return subject;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subject for tenant: {TenantId}", subject.TenantId);
            throw;
        }
    }

    public async Task<Subject?> UpdateSubjectAsync(Subject subject)
    {
        try
        {
            subject.UpdatedAt = DateTime.UtcNow;
            var filter = Builders<Subject>.Filter.And(
                Builders<Subject>.Filter.Eq(s => s.Id, subject.Id),
                Builders<Subject>.Filter.Eq(s => s.TenantId, subject.TenantId));
                
            var result = await _subjects.ReplaceOneAsync(filter, subject);
            return result.MatchedCount > 0 ? subject : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject: {SubjectId}", subject.Id);
            return null;
        }
    }

    public async Task<bool> DeleteSubjectAsync(string tenantId, string subjectId)
    {
        try
        {
            var filter = Builders<Subject>.Filter.And(
                Builders<Subject>.Filter.Eq(s => s.Id, subjectId),
                Builders<Subject>.Filter.Eq(s => s.TenantId, tenantId));
                
            var update = Builders<Subject>.Update
                .Set(s => s.IsActive, false)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);
                
            var result = await _subjects.UpdateOneAsync(filter, update);
            return result.MatchedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject: {SubjectId}", subjectId);
            return false;
        }
    }

    // Reservation management
    public async Task<List<ReservationEntry>> GetAllReservationsAsync(string tenantId)
    {
        try
        {
            var filter = Builders<ReservationEntry>.Filter.Eq(r => r.TenantId, tenantId);
            return await _reservations.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all reservations for tenant: {TenantId}", tenantId);
            return new List<ReservationEntry>();
        }
    }

    public async Task<List<ReservationEntry>> GetReservationsByDeviceAsync(string tenantId, string deviceId)
    {
        try
        {
            var filter = Builders<ReservationEntry>.Filter.And(
                Builders<ReservationEntry>.Filter.Eq(r => r.TenantId, tenantId),
                Builders<ReservationEntry>.Filter.Eq(r => r.DeviceId, deviceId));
                
            return await _reservations.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations for device: {DeviceId}", deviceId);
            return new List<ReservationEntry>();
        }
    }

    public async Task<ReservationEntry> CreateReservationAsync(ReservationEntry reservation)
    {
        try
        {
            reservation.CreatedAt = DateTime.UtcNow;
            // Set expiration based on the reservation date + some buffer time
            reservation.ExpiresAt = reservation.Date.AddHours(1); // Expires 1 hour after reservation time
            
            await _reservations.InsertOneAsync(reservation);
            return reservation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation for tenant: {TenantId}", reservation.TenantId);
            throw;
        }
    }

    public async Task<ReservationEntry?> UpdateReservationAsync(ReservationEntry reservation)
    {
        try
        {
            reservation.UpdatedAt = DateTime.UtcNow;
            reservation.ExpiresAt = reservation.Date.AddHours(1);
            
            var filter = Builders<ReservationEntry>.Filter.And(
                Builders<ReservationEntry>.Filter.Eq(r => r.Id, reservation.Id),
                Builders<ReservationEntry>.Filter.Eq(r => r.TenantId, reservation.TenantId));
                
            var result = await _reservations.ReplaceOneAsync(filter, reservation);
            return result.MatchedCount > 0 ? reservation : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation: {ReservationId}", reservation.Id);
            return null;
        }
    }

    public async Task<bool> DeleteReservationAsync(string tenantId, string reservationId)
    {
        try
        {
            var filter = Builders<ReservationEntry>.Filter.And(
                Builders<ReservationEntry>.Filter.Eq(r => r.Id, reservationId),
                Builders<ReservationEntry>.Filter.Eq(r => r.TenantId, tenantId));
                
            var result = await _reservations.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reservation: {ReservationId}", reservationId);
            return false;
        }
    }

    public async Task<int> CleanupExpiredReservationsAsync(string tenantId)
    {
        try
        {
            var filter = Builders<ReservationEntry>.Filter.And(
                Builders<ReservationEntry>.Filter.Eq(r => r.TenantId, tenantId),
                Builders<ReservationEntry>.Filter.Lt(r => r.ExpiresAt, DateTime.UtcNow));
                
            var result = await _reservations.DeleteManyAsync(filter);
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired reservations for tenant: {TenantId}", tenantId);
            return 0;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
            return true;
        }
        catch
        {
            return false;
        }
    }

    #region ConnectionId-based methods for frontend API
    
    public async Task<ReservationEntry?> GetReservationByConnectionIdAsync(string tenantId, string connectionId)
    {
        try
        {
            var filter = Builders<ReservationEntry>.Filter.And(
                Builders<ReservationEntry>.Filter.Eq(r => r.TenantId, tenantId),
                Builders<ReservationEntry>.Filter.Eq(r => r.ConnectionId, connectionId)
            );

            var reservation = await _reservations.Find(filter).FirstOrDefaultAsync();
            return reservation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservation by ConnectionId: {ConnectionId} for tenant: {TenantId}", connectionId, tenantId);
            return null;
        }
    }

    public async Task<ReservationEntry?> UpdateReservationByConnectionIdAsync(string tenantId, string connectionId, ReservationEntry reservation)
    {
        try
        {
            var filter = Builders<ReservationEntry>.Filter.And(
                Builders<ReservationEntry>.Filter.Eq(r => r.TenantId, tenantId),
                Builders<ReservationEntry>.Filter.Eq(r => r.ConnectionId, connectionId)
            );

            // Ensure we don't change the ConnectionId or TenantId
            reservation.ConnectionId = connectionId;
            reservation.TenantId = tenantId;
            reservation.UpdatedAt = DateTime.UtcNow;

            var result = await _reservations.ReplaceOneAsync(filter, reservation);
            
            if (result.ModifiedCount > 0)
            {
                return reservation;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation by ConnectionId: {ConnectionId} for tenant: {TenantId}", connectionId, tenantId);
            return null;
        }
    }

    public async Task<bool> DeleteReservationByConnectionIdAsync(string tenantId, string connectionId)
    {
        try
        {
            var filter = Builders<ReservationEntry>.Filter.And(
                Builders<ReservationEntry>.Filter.Eq(r => r.TenantId, tenantId),
                Builders<ReservationEntry>.Filter.Eq(r => r.ConnectionId, connectionId)
            );

            var result = await _reservations.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reservation by ConnectionId: {ConnectionId} for tenant: {TenantId}", connectionId, tenantId);
            return false;
        }
    }
    
    #endregion
}