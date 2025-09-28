using LaundrySignalR.Models;

namespace LaundrySignalR.Services;

public class TenantContextService : ITenantContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TenantContextKey = "TenantContext";

    public TenantContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public TenantContext? GetCurrentTenant()
    {
        return _httpContextAccessor.HttpContext?.Items[TenantContextKey] as TenantContext;
    }

    public void SetCurrentTenant(TenantContext tenantContext)
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Items[TenantContextKey] = tenantContext;
        }
    }

    public string GetTenantId()
    {
        return GetCurrentTenant()?.TenantId ?? string.Empty;
    }

    public string GetTenantCode()
    {
        return GetCurrentTenant()?.TenantCode ?? string.Empty;
    }
}