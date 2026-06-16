using Microsoft.Extensions.Diagnostics.HealthChecks;
using LaundrySignalR.Services;

namespace LaundrySignalR;

public class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDbService _mongoDbService;

    public MongoHealthCheck(IMongoDbService mongoDbService)
    {
        _mongoDbService = mongoDbService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _mongoDbService.IsHealthyAsync();
            return isHealthy 
                ? HealthCheckResult.Healthy("MongoDB is healthy") 
                : HealthCheckResult.Unhealthy("MongoDB is not responding");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB health check failed", ex);
        }
    }
}