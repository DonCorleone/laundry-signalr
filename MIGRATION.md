# Laundry SignalR Multi-Tenant Migration Guide

This document outlines the migration from Redis to MongoDB with multi-tenant support.

## Overview

The application has been refactored to support:
- **Multi-tenancy**: Multiple tenants can use the same application instance with data isolation
- **MongoDB Atlas**: Replaced Redis with MongoDB for better cost efficiency
- **Tenant-aware SignalR**: Real-time updates are scoped to specific tenants

## New Architecture

### Multi-Tenancy
- Each tenant has isolated data (subjects and reservations)
- Tenant identification via header, query parameter, or subdomain
- Default tenant for backward compatibility

### Data Models
- `Tenant`: Represents a tenant organization
- `Subject`: Laundry machines (tenant-scoped)
- `ReservationEntry`: Time slot reservations (tenant-scoped)
- All entities include `TenantId` for data isolation

### Tenant Resolution
The application resolves tenants in the following order:
1. `X-Tenant-Code` header
2. `tenant` query parameter
3. Subdomain (e.g., `tenant1.yourdomain.com`)
4. Default tenant (for backward compatibility)

## Configuration

### MongoDB Atlas Setup

1. Create a MongoDB Atlas cluster
2. Create a database user with read/write permissions
3. Get the connection string
4. Update configuration:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://username:password@cluster.mongodb.net/",
    "DatabaseName": "laundry-calendar"
  }
}
```

### Environment Variables (Production)
```bash
MONGODB_CONNECTION_STRING=mongodb+srv://username:password@cluster.mongodb.net/
MONGODB_DATABASE_NAME=laundry-calendar
```

## Migration Process

### 1. Deploy New Version
Deploy the new version with both Redis and MongoDB support for gradual migration.

### 2. Create Default Tenant
```bash
POST /api/tenants
{
  "code": "default",
  "name": "Default Tenant"
}
```

### 3. Migrate Existing Data
Use the migration endpoint to move data from JSON files:

```bash
POST /api/tenants/default/migrate-data
{
  "subjects": [/* your existing subjects */],
  "reservations": [/* any existing reservations */]
}
```

### 4. Update Frontend Applications
Update frontend applications to include tenant identification:

```javascript
// Option 1: Header
fetch('/api/reservations', {
  headers: {
    'X-Tenant-Code': 'your-tenant-code'
  }
});

// Option 2: Query parameter
fetch('/api/reservations?tenant=your-tenant-code');

// SignalR connection
const connection = new HubConnectionBuilder()
  .withUrl('/hub?tenant=your-tenant-code')
  .build();
```

### 5. Create Additional Tenants
For new tenants:

```bash
POST /api/tenants
{
  "code": "tenant2",
  "name": "Tenant 2"
}
```

## API Changes

### New Endpoints
- `GET /api/tenants/{code}` - Get tenant by code
- `POST /api/tenants` - Create new tenant
- `POST /api/tenants/{code}/migrate-data` - Migrate data for tenant
- `POST /api/subjects` - Create subject (tenant-scoped)
- `PUT /api/subjects/{id}` - Update subject (tenant-scoped)
- `DELETE /api/subjects/{id}` - Delete subject (tenant-scoped)
- `GET /api/reservations/device/{deviceId}` - Get reservations by device
- `PUT /api/reservations/{id}` - Update reservation
- `POST /api/reservations/cleanup-expired` - Clean up expired reservations

### Modified Behavior
- All existing endpoints now require tenant context
- Data is automatically filtered by tenant
- SignalR updates are sent only to clients in the same tenant

## SignalR Changes

### Connection
Clients must specify tenant when connecting:
```javascript
const connection = new HubConnectionBuilder()
  .withUrl('/hub?tenant=your-tenant-code')
  .build();
```

### Groups
- Clients are automatically added to tenant-specific groups
- Updates are broadcast only within tenant groups
- Group naming: `tenant_{tenantId}`

## Testing

### Health Check
```bash
GET /health
```

### Tenant Operations
```bash
# Create tenant
POST /api/tenants
{
  "code": "test-tenant",
  "name": "Test Tenant"
}

# Add subjects
POST /api/subjects
Header: X-Tenant-Code: test-tenant
{
  "key": "washer1",
  "name": "Washer 1",
  "icon": "🧽",
  "avatar": "",
  "image": ""
}

# Create reservation
POST /api/reservations
Header: X-Tenant-Code: test-tenant
{
  "name": "John Doe",
  "deviceId": "washer1",
  "date": "2023-12-01T14:00:00Z"
}
```

## Performance Considerations

### Indexes
The MongoDB service automatically creates indexes for:
- Tenant codes (unique)
- Tenant-scoped queries
- TTL for automatic reservation expiration

### Connection Pooling
MongoDB driver handles connection pooling automatically.

### Scaling
- Single instance: No additional configuration needed
- Multiple instances: Consider adding MongoDB-based SignalR backplane

## Monitoring

### Logs
The application logs tenant context with all operations:
```
[INFO] Client connected: connection123 for tenant: tenant1
[INFO] Created reservation for tenant: tenant1
```

### Health Checks
MongoDB health checks available at `/health`

## Cost Comparison

**Before (Redis on Render):**
- Redis instance: $7-25/month
- Limited to single instance

**After (MongoDB Atlas):**
- MongoDB Atlas Free Tier: $0/month (512MB)
- MongoDB Atlas M2: $9/month (2GB)
- Better scaling and features

## Security Considerations

### Tenant Isolation
- All database queries include tenant filters
- Middleware validates tenant existence
- No cross-tenant data leakage possible

### Connection Security
- MongoDB Atlas uses TLS by default
- Connection strings should be stored securely
- Use environment variables in production

## Troubleshooting

### Common Issues

1. **Tenant not found**: Ensure tenant exists and code matches
2. **MongoDB connection**: Check connection string and network access
3. **SignalR not working**: Verify tenant parameter in connection URL

### Debug Logs
Enable debug logging for tenant resolution:
```json
{
  "Logging": {
    "LogLevel": {
      "LaundrySignalR.Middleware.TenantResolverMiddleware": "Debug"
    }
  }
}
```