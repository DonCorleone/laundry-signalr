namespace LaundrySignalR.Models;

public class TenantContext
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}