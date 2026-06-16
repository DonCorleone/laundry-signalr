using LaundrySignalR.Hubs;
using LaundrySignalR.Models;
using LaundrySignalR.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;

namespace LaundrySignalR.Controllers;

/// <summary>
/// Manages laundry reservations with multi-tenant support and real-time notifications
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
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

    /// <summary>
    /// Gets all reservations for the current tenant
    /// </summary>
    /// <returns>List of reservations with frontend-friendly IDs</returns>
    /// <response code="200">Returns the list of reservations</response>
    /// <response code="400">Tenant context not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ReservationResponse>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
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
            var response = reservationEntries.Select(ReservationResponse.FromReservationEntry).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all reservations for a specific device in the current tenant
    /// </summary>
    /// <param name="deviceId">The device identifier (e.g., "WM1", "DRY2")</param>
    /// <returns>List of reservations for the specified device</returns>
    /// <response code="200">Returns the list of reservations for the device</response>
    /// <response code="400">Tenant context not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("device/{deviceId}")]
    [ProducesResponseType(typeof(List<ReservationResponse>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetByDevice(
        [FromRoute] string deviceId)
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            var reservationEntries = await _mongoDbService.GetReservationsByDeviceAsync(tenantId, deviceId);
            var response = reservationEntries.Select(ReservationResponse.FromReservationEntry).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations for device: {DeviceId}", deviceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new reservation or updates an existing one
    /// </summary>
    /// <param name="request">The reservation details</param>
    /// <returns>The created/updated reservation with frontend-friendly ID</returns>
    /// <response code="200">Reservation created or updated successfully</response>
    /// <response code="400">Invalid request data or tenant context not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Post(
        [FromBody] CreateReservationRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            // Parse the date string to DateTime
            if (!DateTime.TryParse(request.Date, out var dateTime))
            {
                return BadRequest("Invalid date format");
            }

            // Use the provided ID as ConnectionId if available, otherwise generate one
            // This ensures frontend tile IDs match backend ConnectionIds
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
                ExpiresAt = dateTime.AddMonths(2) // Auto-expire 2 months after reservation date
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

            // Notify clients in the same tenant via SignalR
            if (reservationEntry != null)
            {
                var tenantGroupName = ReservationHub.GetTenantGroupName(tenantId);
                var signalRResponse = ReservationResponse.FromReservationEntry(reservationEntry);
                
                if (isNewReservation)
                {
                    await _hubContext.Clients.Group(tenantGroupName).ReservationAdded(signalRResponse);
                }
                else
                {
                    await _hubContext.Clients.Group(tenantGroupName).ReservationUpdated(signalRResponse);
                }
            }

            if (reservationEntry != null)
            {
                var response = ReservationResponse.FromReservationEntry(reservationEntry);
                return Ok(response);
            }
            
            return StatusCode(500, "Failed to create/update reservation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating reservation");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates an existing reservation by its ID (ConnectionId)
    /// </summary>
    /// <param name="id">The reservation ID (ConnectionId format: datetime-device)</param>
    /// <param name="request">The updated reservation details</param>
    /// <returns>The updated reservation</returns>
    /// <response code="200">Reservation updated successfully</response>
    /// <response code="400">Invalid request data or tenant context not found</response>
    /// <response code="404">Reservation not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Put(
        [FromRoute] string id, 
        [FromBody] CreateReservationRequest request)
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            // Parse the date string to DateTime
            if (!DateTime.TryParse(request.Date, out var dateTime))
            {
                return BadRequest("Invalid date format");
            }

            // Find existing reservation by ConnectionId (which is the 'id' parameter)
            var existingReservation = await _mongoDbService.GetReservationByConnectionIdAsync(tenantId, id);
            if (existingReservation == null)
            {
                return NotFound("Reservation not found");
            }

            // Update the reservation fields
            existingReservation.Name = request.Name;
            existingReservation.DeviceId = request.DeviceId;
            existingReservation.Date = dateTime;
            existingReservation.ExpiresAt = dateTime.AddMonths(2);
            
            // Update ConnectionId if provided, or generate new one if device/date changed
            if (!string.IsNullOrEmpty(request.ConnectionId))
            {
                existingReservation.ConnectionId = request.ConnectionId;
            }
            else
            {
                // Regenerate ConnectionId if date or device changed
                existingReservation.ConnectionId = $"{dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}-{request.DeviceId}";
            }

            var updatedReservation = await _mongoDbService.UpdateReservationByConnectionIdAsync(tenantId, id, existingReservation);
            if (updatedReservation == null)
            {
                return NotFound("Failed to update reservation");
            }

            // Notify clients in the same tenant via SignalR
            var tenantGroupName = ReservationHub.GetTenantGroupName(tenantId);
            var signalRResponse = ReservationResponse.FromReservationEntry(updatedReservation);
            await _hubContext.Clients.Group(tenantGroupName).ReservationUpdated(signalRResponse);

            var response = ReservationResponse.FromReservationEntry(updatedReservation);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation: {ConnectionId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a reservation by its ID (ConnectionId)
    /// </summary>
    /// <param name="id">The reservation ID (ConnectionId format: datetime-device)</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="200">Reservation deleted successfully</response>
    /// <response code="400">Tenant context not found</response>
    /// <response code="404">Reservation not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Delete(
        [FromRoute] string id)
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            // Delete by ConnectionId (which is the 'id' parameter)
            var removed = await _mongoDbService.DeleteReservationByConnectionIdAsync(tenantId, id);
            if (!removed)
            {
                return NotFound("Reservation not found");
            }

            // Notify clients in the same tenant via SignalR
            var tenantGroupName = ReservationHub.GetTenantGroupName(tenantId);
            await _hubContext.Clients.Group(tenantGroupName).ReservationDeleted(id);

            return Ok(new { id = id }); // Return the ConnectionId as 'id'
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reservation: {ConnectionId}", id);
            return StatusCode(500, "Internal server error");
        }
    }



    /// <summary>
    /// Manually triggers cleanup of expired reservations for the current tenant
    /// </summary>
    /// <returns>Number of reservations deleted</returns>
    /// <response code="200">Cleanup completed successfully</response>
    /// <response code="400">Tenant context not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("cleanup-expired")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
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