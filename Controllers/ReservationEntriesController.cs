using LaundrySignalR.Hubs;
using LaundrySignalR.Models;
using LaundrySignalR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReservationEntriesController : ControllerBase
{
    private readonly IHubContext<ReservationHub, IReservationHubClients> _hubContext;
    private readonly IMongoDbService _mongoDbService;
    private readonly ITenantContextService _tenantContextService;
    private readonly ILogger<ReservationEntriesController> _logger;

    public ReservationEntriesController(
        IHubContext<ReservationHub, IReservationHubClients> hubContext,
        IMongoDbService mongoDbService,
        ITenantContextService tenantContextService,
        ILogger<ReservationEntriesController> logger)
    {
        _hubContext = hubContext;
        _mongoDbService = mongoDbService;
        _tenantContextService = tenantContextService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            var reservationEntries = await _mongoDbService.GetAllReservationsAsync(tenantId);
            return Ok(reservationEntries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("device/{deviceId}")]
    public async Task<IActionResult> GetByDevice(string deviceId)
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            var reservationEntries = await _mongoDbService.GetReservationsByDeviceAsync(tenantId, deviceId);
            return Ok(reservationEntries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations for device: {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ReservationEntry reservationEntry)
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            // Set tenant context for the reservation
            reservationEntry.TenantId = tenantId;

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

            // Notify clients in the same tenant via SignalR
            var tenantGroupName = ReservationHub.GetTenantGroupName(tenantId);
            
            if (isNewReservation)
            {
                await _hubContext.Clients.Group(tenantGroupName).ReservationAdded(reservationEntry);
            }
            else
            {
                await _hubContext.Clients.Group(tenantGroupName).ReservationUpdated(reservationEntry);
            }

            return Ok(reservationEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating reservation");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] ReservationEntry reservationEntry)
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            // Ensure the reservation belongs to the current tenant
            reservationEntry.Id = id;
            reservationEntry.TenantId = tenantId;

            var updatedReservation = await _mongoDbService.UpdateReservationAsync(reservationEntry);
            if (updatedReservation == null)
            {
                return NotFound("Reservation not found");
            }

            // Notify clients in the same tenant via SignalR
            var tenantGroupName = ReservationHub.GetTenantGroupName(tenantId);
            await _hubContext.Clients.Group(tenantGroupName).ReservationUpdated(updatedReservation);

            return Ok(updatedReservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation: {ReservationId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            var removed = await _mongoDbService.DeleteReservationAsync(tenantId, id);
            if (!removed)
            {
                return NotFound("Reservation not found");
            }

            // Notify clients in the same tenant via SignalR
            var tenantGroupName = ReservationHub.GetTenantGroupName(tenantId);
            await _hubContext.Clients.Group(tenantGroupName).ReservationDeleted(id);

            return Ok(new { reservationId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reservation: {ReservationId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("cleanup-expired")]
    public async Task<IActionResult> CleanupExpired()
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            var deletedCount = await _mongoDbService.CleanupExpiredReservationsAsync(tenantId);
            
            _logger.LogInformation("Cleaned up {DeletedCount} expired reservations for tenant {TenantId}", deletedCount, tenantId);
            
            return Ok(new { deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired reservations");
            return StatusCode(500, "Internal server error");
        }
    }
}