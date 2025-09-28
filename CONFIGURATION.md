# Environment Configuration Guide

This document explains how to configure the LaundrySignalR application for different environments.

## Development Setup

### Using User Secrets (Recommended for Development)

The project uses .NET User Secrets to store sensitive configuration locally:

```bash
# Set MongoDB connection string
dotnet user-secrets set "MongoDB:ConnectionString" "your-mongodb-connection-string"

# Set database name
dotnet user-secrets set "MongoDB:DatabaseName" "laundry-calendar-dev"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "MongoDB:ConnectionString"
```

### Using Configuration Files

1. Copy the template files:
   ```bash
   cp appsettings.json.template appsettings.json
   cp appsettings.Development.json.template appsettings.Development.json
   ```

2. Update the connection strings with your actual credentials.

**Note:** The `appsettings.json` files are excluded from git to prevent credential leaks.

## Production/Staging Deployment

### Environment Variables (Recommended for Production)

Set these environment variables in your hosting platform:

```bash
# Required
MONGODB_CONNECTION_STRING=mongodb+srv://username:password@cluster.mongodb.net/
MONGODB_DATABASE_NAME=laundry-calendar

# Optional (with defaults)
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5263
```

### Platform-Specific Setup

#### Render.com
1. Go to your service dashboard
2. Navigate to "Environment" tab
3. Add the environment variables:
   - `MONGODB_CONNECTION_STRING`
   - `MONGODB_DATABASE_NAME`

#### Docker
Create a `.env` file (excluded from git):
```env
MONGODB_CONNECTION_STRING=mongodb+srv://username:password@cluster.mongodb.net/
MONGODB_DATABASE_NAME=laundry-calendar
```

Run with:
```bash
docker run --env-file .env your-image
```

#### Azure App Service
Use Application Settings in the Azure Portal or Azure CLI:
```bash
az webapp config appsettings set --resource-group myResourceGroup --name myAppName --settings MONGODB_CONNECTION_STRING="connection-string-here"
```

#### AWS Elastic Beanstalk
Set environment variables in the EB console or via `.ebextensions/environment.config`:
```yaml
option_settings:
  aws:elasticbeanstalk:application:environment:
    MONGODB_CONNECTION_STRING: "your-connection-string"
    MONGODB_DATABASE_NAME: "laundry-calendar"
```

## Configuration Priority

The application loads configuration in this order (higher priority overrides lower):

1. Environment variables
2. User secrets (Development only)
3. appsettings.{Environment}.json
4. appsettings.json

## MongoDB Atlas Connection String Format

```
mongodb+srv://<username>:<password>@<cluster-name>.mongodb.net/<database-name>?retryWrites=true&w=majority
```

### Security Best Practices

1. **Never commit credentials to git**
2. **Use different databases for different environments**
3. **Rotate credentials regularly**
4. **Use least-privilege database users**
5. **Enable MongoDB Atlas IP whitelist in production**

## Testing Configuration

To verify your configuration is working:

```bash
# Check if secrets are set
dotnet user-secrets list

# Test MongoDB connection
curl http://localhost:5263/health

# Check logs for connection errors
dotnet run --verbosity detailed
```

## Troubleshooting

### Common Issues

1. **"MongoDB connection string is not configured"**
   - Ensure `MONGODB_CONNECTION_STRING` is set
   - Check User Secrets: `dotnet user-secrets list`

2. **Connection timeouts**
   - Verify MongoDB Atlas IP whitelist
   - Check network connectivity
   - Validate connection string format

3. **Authentication failed**
   - Verify username/password in connection string
   - Ensure database user has proper permissions

### Debug Configuration Loading

Add this to see which configuration sources are being used:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Extensions.Configuration": "Debug"
    }
  }
}
```