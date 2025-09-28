# LaundrySignalR - Multi-Tenant Laundry Reservation System

A real-time SignalR application for managing laundry machine reservations with multi-tenant support and MongoDB persistence.

## 🚀 Quick Start

### New Developer Setup

Run the automated setup script:
```bash
./setup-dev.sh
```

This will:
- Create configuration files from templates
- Set up user secrets for MongoDB credentials
- Restore NuGet packages
- Build the project

### Manual Setup

1. **Copy configuration templates:**
   ```bash
   cp appsettings.json.template appsettings.json
   cp appsettings.Development.json.template appsettings.Development.json
   ```

2. **Set up MongoDB credentials using User Secrets:**
   ```bash
   dotnet user-secrets set "MongoDB:ConnectionString" "your-mongodb-connection-string"
   dotnet user-secrets set "MongoDB:DatabaseName" "laundry-calendar-dev"
   ```

3. **Restore and build:**
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Run the application:**
   ```bash
   dotnet run
   ```

## 🏗️ Architecture

### Multi-Tenancy
- Each tenant has isolated data (subjects and reservations)
- Tenant identification via headers, query parameters, or subdomains
- SignalR groups scoped per tenant

### Database
- **Before:** Redis (expensive, volatile)
- **After:** MongoDB Atlas (cost-effective, persistent)

### Real-time Updates
- SignalR for live reservation updates
- Tenant-scoped broadcasting
- Automatic connection management

## 🔧 Configuration

See [CONFIGURATION.md](CONFIGURATION.md) for detailed configuration options.

### Environment Variables (Production)
```bash
MONGODB_CONNECTION_STRING=mongodb+srv://username:password@cluster.mongodb.net/
MONGODB_DATABASE_NAME=laundry-calendar
```

### User Secrets (Development)
```bash
dotnet user-secrets set "MongoDB:ConnectionString" "your-connection-string"
dotnet user-secrets set "MongoDB:DatabaseName" "your-database-name"
```

## 🌐 API Endpoints

### Tenants
- `POST /api/tenants` - Create new tenant
- `GET /api/tenants/{code}` - Get tenant by code
- `POST /api/tenants/{code}/migrate-data` - Migrate existing data

### Subjects (Laundry Machines)
- `GET /api/subjects` - List tenant's subjects
- `POST /api/subjects` - Create subject
- `PUT /api/subjects/{id}` - Update subject
- `DELETE /api/subjects/{id}` - Delete subject

### Reservations
- `GET /api/reservations` - List tenant's reservations
- `GET /api/reservations/device/{deviceId}` - Get device reservations
- `POST /api/reservations` - Create reservation
- `PUT /api/reservations/{id}` - Update reservation
- `DELETE /api/reservations/{id}` - Delete reservation

All endpoints require tenant context via:
- Header: `X-Tenant-Code: your-tenant-code`
- Query parameter: `?tenant=your-tenant-code`
- Subdomain: `tenant-code.yourdomain.com`

## 🔌 SignalR Usage

### Frontend Connection
```javascript
const connection = new HubConnectionBuilder()
  .withUrl('/hub?tenant=your-tenant-code')
  .build();

// Listen for reservation updates
connection.on('ReservationAdded', (reservation) => {
  // Handle new reservation
});

connection.on('ReservationUpdated', (reservation) => {
  // Handle updated reservation
});

connection.on('ReservationDeleted', (reservationId) => {
  // Handle deleted reservation
});
```

## 🐳 Docker

### Build
```bash
docker build --platform linux/amd64 -t doncorleone/laundrysignalr:latest .
```

### Run
```bash
docker run -e MONGODB_CONNECTION_STRING="your-connection-string" -p 5263:5263 doncorleone/laundrysignalr:latest
```

### Registry
[hub.docker.com](https://hub.docker.com/r/doncorleone/laundrysignalr/tags)

## 📋 Migration from Redis

See [MIGRATION.md](MIGRATION.md) for complete migration guide.

### Key Steps:
1. Set up MongoDB Atlas
2. Create default tenant
3. Migrate existing data
4. Update frontend applications
5. Remove Redis dependencies

## 🧪 Testing

### Health Check
```bash
curl http://localhost:5263/health
```

### Create Test Tenant
```bash
curl -X POST http://localhost:5263/api/tenants \
  -H "Content-Type: application/json" \
  -d '{"code": "test", "name": "Test Tenant"}'
```

## 🔍 Troubleshooting

### Configuration Issues
```bash
# Check user secrets
dotnet user-secrets list

# Verify configuration loading
dotnet run --verbosity detailed
```

### Common Problems
- **MongoDB connection failed:** Check connection string and network access
- **Tenant not found:** Ensure tenant exists and code is correct
- **SignalR not connecting:** Verify tenant parameter in connection URL

## 🤝 Development

### Project Structure
```
├── Controllers/          # API controllers
├── Hubs/                # SignalR hubs
├── Middleware/          # Tenant resolution middleware
├── Models/              # Data models
├── Services/            # Business services
├── Scripts/             # Migration scripts
└── HealthChecks/        # Health check implementations
```

### Adding New Features
1. Consider multi-tenancy in all data operations
2. Include tenant context in logging
3. Test with multiple tenants
4. Update documentation

## 📜 License

[Your License Here]