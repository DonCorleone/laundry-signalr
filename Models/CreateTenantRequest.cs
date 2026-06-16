using System.ComponentModel.DataAnnotations;

namespace LaundrySignalR.Models;

/// <summary>
/// Request model for creating a new tenant (building/house)
/// </summary>
public class CreateTenantRequest
{
    /// <summary>
    /// Tenant display name (e.g., "Building A", "John's House")
    /// </summary>
    /// <example>John's Apartment Building</example>
    [Required(ErrorMessage = "Tenant name is required")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique tenant code for URL/identification (e.g., "building-a", "johns-house")
    /// </summary>
    /// <example>johns-house</example>
    [Required(ErrorMessage = "Tenant code is required")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Tenant code can only contain lowercase letters, numbers, and hyphens")]
    public string Code { get; set; } = string.Empty;
}