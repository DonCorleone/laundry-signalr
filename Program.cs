using LaundrySignalR.Hubs;
using LaundrySignalR.Services;
using LaundrySignalR.Middleware;
using LaundrySignalR;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add CORS with dynamic subdomain support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient",
        b =>
        {
            b.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin))
                    return false;

                var uri = new Uri(origin);
                
                // Allow specific known origins
                var allowedOrigins = new[]
                {
                    "https://laundry-reservation.onrender.com",
                    "https://slotwi.se"
                };

                if (allowedOrigins.Contains(origin))
                    return true;

                // Allow any subdomain of slotwi.se
                if (uri.Host.EndsWith(".slotwi.se", StringComparison.OrdinalIgnoreCase) &&
                    uri.Scheme == "https")
                    return true;

                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});

// Add controllers and configure JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddHttpContextAccessor();

// Add Swagger/OpenAPI services (only in development for security)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Laundry SignalR API",
            Version = "v1",
            Description = "Multi-tenant laundry reservation system with real-time SignalR notifications",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Laundry Calendar",
                Email = "vitocorleone77@gmail.com"
            }
        });
        
        // Configure Swagger to use the same JSON naming policy
        c.DescribeAllParametersInCamelCase();
    });
}

// Configure MongoDB
var configuration = builder.Configuration;
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
    ?? configuration.GetConnectionString("MongoDB")
    ?? configuration["MongoDB:ConnectionString"];

var mongoDatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
    ?? "laundry-calendar";

if (string.IsNullOrEmpty(mongoConnectionString))
{
    throw new InvalidOperationException("MongoDB connection string is not configured. Please set it in appsettings.json or environment variables.");
}

builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
});

// Register services
builder.Services.AddSingleton<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<ITenantContextService, TenantContextService>();

// Keep JSON file service for backward compatibility during migration
builder.Services.AddSingleton<IJsonFileService, JsonFileService>();

// Add SignalR without Redis (for single-instance deployment)
// For multi-instance, you can add MongoDB backplane later
builder.Services.AddSignalR();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<MongoHealthCheck>("mongodb");

var app = builder.Build();

// Configure Swagger UI (only in development for security)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Laundry SignalR API v1");
        c.RoutePrefix = "swagger"; // Access at /swagger
        c.DocumentTitle = "Laundry SignalR API Documentation - Development";
        c.DefaultModelsExpandDepth(-1); // Hide models by default for cleaner UI
    });
}

// Configure middleware pipeline
app.UseCors("AllowAngularClient"); // CORS must come before UseRouting for SignalR
app.UseRouting();

// Add tenant resolver middleware
app.UseMiddleware<TenantResolverMiddleware>();

// Map endpoints
app.MapControllers();
app.MapHub<ReservationHub>("/hub").RequireCors("AllowAngularClient");
app.MapHealthChecks("/health");

// Explicitly set the port to 5263
app.Run("http://0.0.0.0:5263");