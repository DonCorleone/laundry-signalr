using LaundrySignalR.Models;

namespace LaundrySignalR.Services;

public interface ITenantContextService
{
    TenantContext? GetCurrentTenant();
    void SetCurrentTenant(TenantContext tenantContext);
    string GetTenantId();
    string GetTenantCode();
}