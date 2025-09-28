using LaundrySignalR.Models;
using LaundrySignalR.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaundrySignalR.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SubjectsController : ControllerBase
{
    private readonly IMongoDbService _mongoDbService;
    private readonly ITenantContextService _tenantContextService;
    private readonly ILogger<SubjectsController> _logger;

    public SubjectsController(
        IMongoDbService mongoDbService,
        ITenantContextService tenantContextService,
        ILogger<SubjectsController> logger)
    {
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

            var subjects = await _mongoDbService.GetSubjectsAsync(tenantId);
            return Ok(subjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subjects");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Subject subject)
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            // Set tenant context for the subject
            subject.TenantId = tenantId;

            var createdSubject = await _mongoDbService.CreateSubjectAsync(subject);
            return CreatedAtAction(nameof(Get), new { id = createdSubject.Id }, createdSubject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subject");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] Subject subject)
    {
        try
        {
            var tenantId = _tenantContextService.GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest("Tenant context not found");
            }

            // Ensure the subject belongs to the current tenant
            subject.Id = id;
            subject.TenantId = tenantId;

            var updatedSubject = await _mongoDbService.UpdateSubjectAsync(subject);
            if (updatedSubject == null)
            {
                return NotFound("Subject not found");
            }

            return Ok(updatedSubject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject: {SubjectId}", id);
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

            var deleted = await _mongoDbService.DeleteSubjectAsync(tenantId, id);
            if (!deleted)
            {
                return NotFound("Subject not found");
            }

            return Ok(new { subjectId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject: {SubjectId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}