# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LaundrySignalR is a real-time multi-tenant laundry machine reservation system built with ASP.NET Core 9.0, SignalR, and MongoDB. It manages reservations for multiple tenants with isolated data and real-time updates via SignalR hubs.

## Development Commands

### Initial Setup
```bash
dotnet restore
dotnet build
```

### MongoDB Configuration (Required)
```bash
# Development: Use User Secrets
dotnet user-secrets set "MongoDB:ConnectionString" "your-mongodb-connection-string"
dotnet user-secrets set "MongoDB:DatabaseName" "laundry-calendar-dev"

# Production: Use environment variables
export MONGODB_CONNECTION_STRING="mongodb+srv://..."
export MONGODB_DATABASE_NAME="laundry-calendar"
```

### Run Application
```bash
dotnet run                    # Development mode (includes Swagger at /swagger)
dotnet watch run              # With hot reload
```

### Docker
```bash
# Build (platform-specific for production)
docker build --platform linux/amd64 -t doncorleone/laundrysignalr:latest .

# Run locally
docker run -e MONGODB_CONNECTION_STRING="your-string" -p 5263:5263 doncorleone/laundrysignalr:latest
```

### Testing
```bash
# Health check
curl http://localhost:5263/health

# Create test tenant
curl -X POST http://localhost:5263/api/tenants \
  -H "Content-Type: application/json" \
  -d '{"code": "test", "name": "Test Tenant"}'
```

## Architecture & Key Concepts

### Multi-Tenancy System
**Critical**: Every data operation MUST be tenant-scoped. Tenant isolation is enforced at three layers:

1. **Tenant Resolution** (TenantResolverMiddleware.cs:17)
   - Resolves tenant from: `X-Tenant-Code` header → query parameter `?tenant=code` → subdomain extraction
   - Automatically creates "default" tenant for backward compatibility during migration
   - Sets TenantContext for the request lifetime via ITenantContextService

2. **Data Layer** (MongoDbService.cs)
   - All MongoDB queries filter by TenantId
   - Indexes ensure performance: compound indexes on (TenantId, DeviceId), (TenantId, Key)
   - Collections: `tenants`, `subjects`, `reservations`

3. **SignalR Layer** (ReservationHub.cs:23-37)
   - Clients automatically join tenant-specific SignalR groups: `tenant_{tenantId}`
   - All broadcasts scoped to: `Clients.Group($"tenant_{tenantId}")`
   - Tenant identified via query string: `/hub?tenant=your-tenant-code`

### SignalR Hub Methods
Located in ReservationHub.cs:

- `OnConnectedAsync()` - Auto-joins tenant group, loads initial reservations
- `CreateReservation(CreateReservationRequest)` - Creates/updates reservation, broadcasts to tenant group
- `DeleteReservation(string reservationId)` - Deletes by ConnectionId, broadcasts deletion
- Client events: `ReservationsLoaded`, `ReservationAdded`, `ReservationUpdated`, `ReservationDeleted`

### MongoDB Schema & TTL
- **TTL Index**: Reservations auto-expire via MongoDB TTL index on `ExpiresAt` field (MongoDbService.cs:54-57)
- Default expiration: 2 months after reservation date (MongoDbService.cs:210)
- Soft deletes for Subjects (IsActive flag), hard deletes for Reservations

### ConnectionId vs ID Pattern
**Important**: The system uses `ConnectionId` as the business identifier for reservations (not MongoDB ObjectId):
- Frontend uses stable identifiers like `"{date}-{deviceId}"` as ConnectionId
- MongoDB ObjectId (`Id`) is internal only
- CRUD operations by ConnectionId: `GetReservationByConnectionIdAsync()`, `UpdateReservationByConnectionIdAsync()`, `DeleteReservationByConnectionIdAsync()` (MongoDbService.cs:292-362)

## Configuration

### CORS Policy (Program.cs:10-44)
- Allows specific origins: `laundry-reservation.onrender.com`, `slotwi.se`, localhost:4200/4000
- Dynamic subdomain support: all `*.slotwi.se` subdomains allowed
- Credentials enabled for SignalR

### Environment-Specific Behavior
- **Development**: Swagger UI enabled at `/swagger`
- **Production**: Swagger disabled for security (Program.cs:118-129)
- Port: Always 5263 (Program.cs:144)

## Project Structure

```
├── Controllers/          # REST API endpoints (Tenants, Subjects, Reservations)
├── Hubs/                # SignalR hub (ReservationHub) and client interfaces
├── Middleware/          # TenantResolverMiddleware - tenant identification
├── Models/              # Data models (Tenant, Subject, ReservationEntry, etc.)
├── Services/            # Business logic (MongoDbService, TenantContextService)
├── HealthChecks/        # MongoDB health check for /health endpoint
└── Scripts/             # Migration scripts (Redis → MongoDB)
```

## Important Implementation Notes

### When Adding Features
1. **Always filter by TenantId** in database queries
2. **Use ITenantContextService** to get current tenant context
3. **Test with multiple tenants** to ensure isolation
4. **For SignalR broadcasts**, use `Clients.Group($"tenant_{tenantId}")`, never `Clients.All`
5. **Set ExpiresAt** for any time-based data to leverage MongoDB TTL

### Logging
All operations log with tenant context. Use structured logging:
```csharp
_logger.LogInformation("Operation for tenant: {TenantId}", tenantId);
```

### API Requirements
All API endpoints require tenant identification via:
- Header: `X-Tenant-Code: tenant-code`
- Query: `?tenant=tenant-code`
- Subdomain: `tenant-code.slotwi.se`

Missing or invalid tenant returns 404.

## Migration Context

This system migrated from Redis to MongoDB. The JsonFileService remains for backward compatibility but is not actively used. Default tenant ("default") is auto-created for legacy data migration.
