using LaundrySignalR.Models;
using LaundrySignalR.Services;
using System.Text.Json;

namespace LaundrySignalR.Scripts;

public class MigrationScript
{
    private readonly IMongoDbService _mongoDbService;
    private readonly IJsonFileService _jsonFileService;
    private readonly ILogger<MigrationScript> _logger;

    public MigrationScript(
        IMongoDbService mongoDbService, 
        IJsonFileService jsonFileService, 
        ILogger<MigrationScript> logger)
    {
        _mongoDbService = mongoDbService;
        _jsonFileService = jsonFileService;
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateFromJsonToMongoDb(string tenantCode = "default")
    {
        var result = new MigrationResult();

        try
        {
            // Ensure default tenant exists
            var tenant = await _mongoDbService.GetTenantByCodeAsync(tenantCode);
            if (tenant == null)
            {
                tenant = await _mongoDbService.CreateTenantAsync(new Tenant
                {
                    Code = tenantCode,
                    Name = "Default Tenant",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                _logger.LogInformation("Created tenant: {TenantCode}", tenantCode);
            }

            // Migrate subjects from JSON file
            var jsonSubjects = await _jsonFileService.LoadSubjects();
            if (jsonSubjects?.Any() == true)
            {
                foreach (var jsonSubject in jsonSubjects)
                {
                    try
                    {
                        var subject = new Subject
                        {
                            Key = jsonSubject.Key,
                            Name = jsonSubject.Name,
                            Icon = jsonSubject.Icon,
                            Avatar = jsonSubject.Avatar,
                            Image = jsonSubject.Image,
                            TenantId = tenant.Id,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        await _mongoDbService.CreateSubjectAsync(subject);
                        result.SubjectsMigrated++;
                        _logger.LogInformation("Migrated subject: {SubjectKey}", subject.Key);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Failed to migrate subject {jsonSubject.Key}: {ex.Message}");
                        _logger.LogError(ex, "Error migrating subject: {SubjectKey}", jsonSubject.Key);
                    }
                }
            }

            result.TenantId = tenant.Id;
            result.Success = true;

            _logger.LogInformation("Migration completed for tenant {TenantCode}: {SubjectCount} subjects migrated", 
                tenantCode, result.SubjectsMigrated);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Migration failed: {ex.Message}");
            _logger.LogError(ex, "Migration failed for tenant: {TenantCode}", tenantCode);
        }

        return result;
    }

    public async Task<MigrationResult> MigrateRedisDataToMongoDB(
        string tenantCode,
        Dictionary<string, Dictionary<string, string>> redisData)
    {
        var result = new MigrationResult();

        try
        {
            // Ensure tenant exists
            var tenant = await _mongoDbService.GetTenantByCodeAsync(tenantCode);
            if (tenant == null)
            {
                result.Success = false;
                result.Errors.Add($"Tenant '{tenantCode}' not found");
                return result;
            }

            // Migrate Redis hash data to MongoDB reservations
            foreach (var deviceData in redisData)
            {
                var deviceId = deviceData.Key;
                var reservations = deviceData.Value;

                foreach (var reservation in reservations)
                {
                    try
                    {
                        var reservationId = reservation.Key;
                        var reservationName = reservation.Value;

                        // Parse date from reservation ID (assuming format includes timestamp)
                        var reservationDate = ParseDateFromReservationId(reservationId);

                        var reservationEntry = new ReservationEntry
                        {
                            Id = reservationId,
                            Name = reservationName,
                            DeviceId = deviceId,
                            TenantId = tenant.Id,
                            Date = reservationDate,
                            CreatedAt = DateTime.UtcNow,
                            ExpiresAt = reservationDate.AddHours(1)
                        };

                        await _mongoDbService.CreateReservationAsync(reservationEntry);
                        result.ReservationsMigrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Failed to migrate reservation {reservation.Key}: {ex.Message}");
                        _logger.LogError(ex, "Error migrating reservation: {ReservationId}", reservation.Key);
                    }
                }
            }

            result.TenantId = tenant.Id;
            result.Success = true;

            _logger.LogInformation("Redis migration completed for tenant {TenantCode}: {ReservationCount} reservations migrated", 
                tenantCode, result.ReservationsMigrated);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Redis migration failed: {ex.Message}");
            _logger.LogError(ex, "Redis migration failed for tenant: {TenantCode}", tenantCode);
        }

        return result;
    }

    private DateTime ParseDateFromReservationId(string reservationId)
    {
        try
        {
            // Try to extract date from reservation ID
            // Assuming format like "2023-10-27T14:30:00.000Z_something"
            if (reservationId.Length >= 24)
            {
                var datePart = reservationId.Substring(0, 24);
                if (DateTime.TryParse(datePart, out var parsedDate))
                {
                    return parsedDate;
                }
            }

            // Fallback to current time + 1 hour
            return DateTime.UtcNow.AddHours(1);
        }
        catch
        {
            return DateTime.UtcNow.AddHours(1);
        }
    }
}

public class MigrationResult
{
    public bool Success { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public int SubjectsMigrated { get; set; }
    public int ReservationsMigrated { get; set; }
    public List<string> Errors { get; set; } = new();
}