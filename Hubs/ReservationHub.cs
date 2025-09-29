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

    // Helper method to get tenant group name for broadcasting
    public static string GetTenantGroupName(string tenantId) => $"tenant_{tenantId}";
}