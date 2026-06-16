using LaundrySignalR.Models;

namespace LaundrySignalR.Services;

public interface IMongoDbService
{
    // Tenant management
    Task<Tenant?> GetTenantByCodeAsync(string tenantCode);
    Task<Tenant> CreateTenantAsync(Tenant tenant);
    
    // Subject management (per tenant)
    Task<List<Subject>> GetSubjectsAsync(string tenantId);
    Task<Subject> CreateSubjectAsync(Subject subject);
    Task<Subject?> UpdateSubjectAsync(Subject subject);
    Task<bool> DeleteSubjectAsync(string tenantId, string subjectId);
    
    // Reservation management (per tenant)
    Task<List<ReservationEntry>> GetAllReservationsAsync(string tenantId);
    Task<List<ReservationEntry>> GetReservationsByDeviceAsync(string tenantId, string deviceId);
    Task<ReservationEntry> CreateReservationAsync(ReservationEntry reservation);
    Task<ReservationEntry?> UpdateReservationAsync(ReservationEntry reservation);
    Task<bool> DeleteReservationAsync(string tenantId, string reservationId);
    Task<int> CleanupExpiredReservationsAsync(string tenantId);
    
    // ConnectionId-based methods for frontend API
    Task<ReservationEntry?> GetReservationByConnectionIdAsync(string tenantId, string connectionId);
    Task<ReservationEntry?> UpdateReservationByConnectionIdAsync(string tenantId, string connectionId, ReservationEntry reservation);
    Task<bool> DeleteReservationByConnectionIdAsync(string tenantId, string connectionId);
    
    // Health check
    Task<bool> IsHealthyAsync();
}