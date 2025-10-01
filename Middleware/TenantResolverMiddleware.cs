using LaundrySignalR.Models;
using LaundrySignalR.Services;

namespace LaundrySignalR.Middleware;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolverMiddleware> _logger;

    public TenantResolverMiddleware(RequestDelegate next, ILogger<TenantResolverMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContextService tenantContext, IMongoDbService mongoDbService)
    {
        string? tenantCode = null;

        // Try to get tenant from header first
        if (context.Request.Headers.TryGetValue("X-Tenant-Code", out var headerValue))
        {
            tenantCode = headerValue.FirstOrDefault();
        }

        // Fallback: try to get tenant from query parameter
        if (string.IsNullOrEmpty(tenantCode))
        {
            tenantCode = context.Request.Query["tenant"].FirstOrDefault();
        }

        // Fallback: try to get tenant from subdomain (e.g., tenant1.yourdomain.com)
        // Skip subdomain extraction for Render and localhost
        if (string.IsNullOrEmpty(tenantCode))
        {
            var host = context.Request.Host.Host;
            var parts = host.Split('.');
            
            // Skip subdomain extraction for Render domains and localhost
            bool isRenderDomain = host.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase);
            bool isLocalhost = host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase);
            
            if (parts.Length > 2 && !isRenderDomain && !isLocalhost)
            {
                tenantCode = parts[0];
            }
        }

        // Default tenant for backward compatibility during migration
        if (string.IsNullOrEmpty(tenantCode))
        {
            tenantCode = "default";
        }
        
        // Force default tenant for deployment domains
        var currentHost = context.Request.Host.Host;
        if (currentHost.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase))
        {
            tenantCode = "default";
        }

        try
        {
            // Resolve tenant from database
            var tenant = await mongoDbService.GetTenantByCodeAsync(tenantCode);
            
            if (tenant == null)
            {
                // Create default tenant if it doesn't exist (for migration)
                if (tenantCode == "default")
                {
                    tenant = await mongoDbService.CreateTenantAsync(new Tenant
                    {
                        Name = "Default Tenant",
                        Code = "default",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                    _logger.LogInformation("Created default tenant for migration");
                }
                else
                {
                    _logger.LogWarning("Tenant not found: {TenantCode}", tenantCode);
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"Tenant '{tenantCode}' not found");
                    return;
                }
            }

            // Set tenant context
            tenantContext.SetCurrentTenant(new TenantContext
            {
                TenantId = tenant.Id,
                TenantCode = tenant.Code,
                TenantName = tenant.Name
            });

            _logger.LogDebug("Resolved tenant: {TenantCode} ({TenantId})", tenant.Code, tenant.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant: {TenantCode}", tenantCode);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Error resolving tenant");
            return;
        }

        await _next(context);
    }
}