using LaundrySignalR.Models;
using LaundrySignalR.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaundrySignalR.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TenantsController : ControllerBase
{
    private readonly IMongoDbService _mongoDbService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(IMongoDbService mongoDbService, ILogger<TenantsController> logger)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.Name))
            {
                return BadRequest("Tenant code and name are required");
            }

            // Check if tenant already exists
            var existingTenant = await _mongoDbService.GetTenantByCodeAsync(request.Code);
            if (existingTenant != null)
            {
                return Conflict($"Tenant with code '{request.Code}' already exists");
            }

            var tenant = new Tenant
            {
                Code = request.Code,
                Name = request.Name,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createdTenant = await _mongoDbService.CreateTenantAsync(tenant);
            
            _logger.LogInformation("Created new tenant: {TenantCode} ({TenantId})", createdTenant.Code, createdTenant.Id);
            
            return CreatedAtAction(nameof(GetTenant), new { code = createdTenant.Code }, createdTenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetTenant(string code)
    {
        try
        {
            var tenant = await _mongoDbService.GetTenantByCodeAsync(code);
            if (tenant == null)
            {
                return NotFound($"Tenant with code '{code}' not found");
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant: {TenantCode}", code);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{tenantCode}/migrate-data")]
    public async Task<IActionResult> MigrateData(string tenantCode, [FromBody] MigrateDataRequest request)
    {
        try
        {
            var tenant = await _mongoDbService.GetTenantByCodeAsync(tenantCode);
            if (tenant == null)
            {
                return NotFound($"Tenant '{tenantCode}' not found");
            }

            var migrationResults = new
            {
                TenantId = tenant.Id,
                SubjectsMigrated = 0,
                ReservationsMigrated = 0,
                Errors = new List<string>()
            };

            // Migrate subjects if provided
            if (request.Subjects?.Any() == true)
            {
                foreach (var subject in request.Subjects)
                {
                    try
                    {
                        subject.TenantId = tenant.Id;
                        subject.CreatedAt = DateTime.UtcNow;
                        await _mongoDbService.CreateSubjectAsync(subject);
                        migrationResults = migrationResults with { SubjectsMigrated = migrationResults.SubjectsMigrated + 1 };
                    }
                    catch (Exception ex)
                    {
                        var errorList = migrationResults.Errors.ToList();
                        errorList.Add($"Failed to migrate subject {subject.Key}: {ex.Message}");
                        migrationResults = migrationResults with { Errors = errorList };
                    }
                }
            }

            // Migrate reservations if provided
            if (request.Reservations?.Any() == true)
            {
                foreach (var reservation in request.Reservations)
                {
                    try
                    {
                        reservation.TenantId = tenant.Id;
                        reservation.CreatedAt = DateTime.UtcNow;
                        reservation.ExpiresAt = reservation.Date.AddHours(1);
                        await _mongoDbService.CreateReservationAsync(reservation);
                        migrationResults = migrationResults with { ReservationsMigrated = migrationResults.ReservationsMigrated + 1 };
                    }
                    catch (Exception ex)
                    {
                        var errorList = migrationResults.Errors.ToList();
                        errorList.Add($"Failed to migrate reservation {reservation.Id}: {ex.Message}");
                        migrationResults = migrationResults with { Errors = errorList };
                    }
                }
            }

            _logger.LogInformation("Migration completed for tenant {TenantCode}: {SubjectCount} subjects, {ReservationCount} reservations", 
                tenantCode, migrationResults.SubjectsMigrated, migrationResults.ReservationsMigrated);

            return Ok(migrationResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating data for tenant: {TenantCode}", tenantCode);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class CreateTenantRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class MigrateDataRequest
{
    public List<Subject>? Subjects { get; set; }
    public List<ReservationEntry>? Reservations { get; set; }
}