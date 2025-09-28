using LaundrySignalR.Hubs;
using LaundrySignalR.Services;
using LaundrySignalR.Middleware;
using LaundrySignalR;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient",
        b =>
        {
            b.WithOrigins([
                    "http://localhost:4000", 
                    "https://laundry-calendar.netlify.app", 
                    "https://laundry-reservation.onrender.com",
                    "https://server-mock--laundry-calendar.netlify.app/"
                ])
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Add controllers and other services
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Configure MongoDB
var configuration = builder.Configuration;
var mongoConnectionString = configuration.GetConnectionString("MongoDB") 
    ?? configuration["MongoDB:ConnectionString"] 
    ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");

var mongoDatabaseName = configuration["MongoDB:DatabaseName"] 
    ?? Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME") 
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

// Configure middleware pipeline
app.UseRouting();
app.UseCors("AllowAngularClient");

// Add tenant resolver middleware
app.UseMiddleware<TenantResolverMiddleware>();

// Map endpoints
app.MapControllers();
app.MapHub<ReservationHub>("/hub");
app.MapHealthChecks("/health");

// Explicitly set the port to 5263
app.Run("http://0.0.0.0:5263");