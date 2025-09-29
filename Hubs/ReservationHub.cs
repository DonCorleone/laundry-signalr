using LaundrySignalR.Models;
using LaundrySignalR.Services;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Hubs;

public class ReservationHub : Hub<IReservationHubClients>
{
    private readonly ILogger<ReservationHub> _logger;
    private readonly IMongoDbService _mongoDbService;
    private readonly ITenantContextService _tenantContextService;

    public ReservationHub(
        ILogger<ReservationHub> logger, 
        IMongoDbService mongoDbService,
        ITenantContextService tenantContextService)
    {
        _logger = logger;
        _mongoDbService = mongoDbService;
        _tenantContextService = tenantContextService;
    }

    public override async Task OnConnectedAsync()
    {
        var tenantId = GetTenantIdFromQuery();
        
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("Client connected without tenant context: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        _logger.LogInformation("Client connected: {ConnectionId} for tenant: {TenantId}", Context.ConnectionId, tenantId);

        // Add to tenant-specific group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");

        try
        {
            // Load tenant-specific data
            var reservationEntries = await _mongoDbService.GetAllReservationsAsync(tenantId);
            var responseData = reservationEntries.Select(ReservationResponse.FromReservationEntry).ToList();
            
            // Send initial data to the connected client
            await Clients.Caller.ReservationsLoaded(responseData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reservations for tenant {TenantId}", tenantId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = GetTenantIdFromQuery();
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            _logger.LogInformation("Client disconnected: {ConnectionId} from tenant: {TenantId}", Context.ConnectionId, tenantId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Hub method to join a specific tenant group (alternative to query string)
    public async Task JoinTenantGroup(string tenantCode)
    {
        try
        {
            var tenant = await _mongoDbService.GetTenantByCodeAsync(tenantCode);
            if (tenant == null)
            {
                _logger.LogWarning("Invalid tenant code in JoinTenantGroup: {TenantCode}", tenantCode);
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenant.Id}");
            _logger.LogInformation("Client {ConnectionId} joined tenant group: {TenantId}", Context.ConnectionId, tenant.Id);
            
            // Send current reservations for this tenant
            var reservationEntries = await _mongoDbService.GetAllReservationsAsync(tenant.Id);
            var responseData = reservationEntries.Select(ReservationResponse.FromReservationEntry).ToList();
            await Clients.Caller.ReservationsLoaded(responseData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining tenant group for code: {TenantCode}", tenantCode);
        }
    }

    private string? GetTenantIdFromQuery()
    {
        // Try to get tenant from query string
        if (Context.GetHttpContext()?.Request.Query.TryGetValue("tenant", out var tenantCode) == true)
        {
            var code = tenantCode.FirstOrDefault();
            if (!string.IsNullOrEmpty(code))
            {
                // In a real implementation, you might want to cache this lookup
                var tenant = _mongoDbService.GetTenantByCodeAsync(code).Result;
                return tenant?.Id;
            }
        }

        // Fallback to default tenant for migration compatibility
        var defaultTenant = _mongoDbService.GetTenantByCodeAsync("default").Result;
        return defaultTenant?.Id;
    }

    // Hub method to create a new reservation
    public async Task CreateReservation(CreateReservationRequest request)
    {
        try
        {
            _logger.LogInformation("CreateReservation called by {ConnectionId} with data: {@Request}", Context.ConnectionId, request);
            
            var tenantId = GetTenantIdFromQuery();
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("CreateReservation called without tenant context: {ConnectionId}", Context.ConnectionId);
                return;
            }
            
            _logger.LogInformation("CreateReservation processing for tenant: {TenantId}", tenantId);

            // Parse the date string to DateTime
            if (!DateTime.TryParse(request.Date, out var dateTime))
            {
                _logger.LogWarning("Invalid date format in CreateReservation: {Date}", request.Date);
                return;
            }

            // Use the provided ID as ConnectionId if available, otherwise generate one
            var connectionId = !string.IsNullOrEmpty(request.Id) 
                ? request.Id
                : !string.IsNullOrEmpty(request.ConnectionId) 
                    ? request.ConnectionId
                    : $"{dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}-{request.DeviceId}";

            // Create the full ReservationEntry with server-managed fields
            var reservationEntry = new ReservationEntry
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = request.Name,
                DeviceId = request.DeviceId,
                ConnectionId = connectionId,
                Date = dateTime,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = dateTime.AddMonths(1)
            };

            // Try to update first (in case this is an existing reservation)
            var updatedReservation = await _mongoDbService.UpdateReservationAsync(reservationEntry);
            bool isNewReservation = updatedReservation == null;

            if (isNewReservation)
            {
                // Create new reservation
                reservationEntry = await _mongoDbService.CreateReservationAsync(reservationEntry);
            }
            else
            {
                reservationEntry = updatedReservation;
            }

            // Broadcast to all clients in the same tenant group
            if (reservationEntry != null)
            {
                var tenantGroupName = GetTenantGroupName(tenantId);
                var signalRResponse = ReservationResponse.FromReservationEntry(reservationEntry);
                
                if (isNewReservation)
                {
                    await Clients.Group(tenantGroupName).ReservationAdded(signalRResponse);
                    _logger.LogInformation("Reservation added via SignalR: {ConnectionId} for tenant: {TenantId}", connectionId, tenantId);
                }
                else
                {
                    await Clients.Group(tenantGroupName).ReservationUpdated(signalRResponse);
                    _logger.LogInformation("Reservation updated via SignalR: {ConnectionId} for tenant: {TenantId}", connectionId, tenantId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation via SignalR");
        }
    }

    // Hub method to delete a reservation
    public async Task DeleteReservation(string reservationId)
    {
        try
        {
            _logger.LogInformation("DeleteReservation called by {ConnectionId} for reservation: {ReservationId}", Context.ConnectionId, reservationId);
            
            var tenantId = GetTenantIdFromQuery();
            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("DeleteReservation called without tenant context: {ConnectionId}", Context.ConnectionId);
                return;
            }
            
            _logger.LogInformation("DeleteReservation processing for tenant: {TenantId}", tenantId);

            // Delete by ConnectionId (which is the reservationId parameter)
            var removed = await _mongoDbService.DeleteReservationByConnectionIdAsync(tenantId, reservationId);
            if (removed)
            {
                // Broadcast to all clients in the same tenant group
                var tenantGroupName = GetTenantGroupName(tenantId);
                await Clients.Group(tenantGroupName).ReservationDeleted(reservationId);
                _logger.LogInformation("Reservation deleted via SignalR: {ConnectionId} for tenant: {TenantId}", reservationId, tenantId);
            }
            else
            {
                _logger.LogWarning("Reservation not found for deletion: {ConnectionId} for tenant: {TenantId}", reservationId, tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reservation via SignalR: {ReservationId}", reservationId);
        }
    }

    // Helper method to get tenant group name for broadcasting
    public static string GetTenantGroupName(string tenantId) => $"tenant_{tenantId}";
}