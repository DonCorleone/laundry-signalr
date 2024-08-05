
using System.Net;
using LaundrySignalR.Hubs;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient",
        b =>
        {
            b.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});
builder.Services
    .AddSignalR()
    .AddStackExchangeRedis(
        "redis://red-cqnqmfg8fa8c73at4hog:6379",
        o =>
        {
            o.ConnectionFactory = async writer =>
            {
                var config = new ConfigurationOptions
                {
                    AbortOnConnectFail = false
                };
                config.EndPoints.Add(IPAddress.Loopback, 0);
                config.SetDefaultPorts();
                var connection = await ConnectionMultiplexer.ConnectAsync(config, writer);
                connection.ConnectionFailed += (_, e) =>
                {
                    Console.WriteLine("Connection to Redis failed.");
                };

                if (!connection.IsConnected)
                {
                    Console.WriteLine("Did not connect to Redis.");
                }

                return connection;
            };
            o.Configuration.AbortOnConnectFail = false;
        });
var app = builder.Build();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
// Configure the HTTP request pipeline.
app.UseCors("AllowAngularClient");

app.MapHub<ChatHub>("/hub");

// Explicitly set the port to 5263
app.Run("http://0.0.0.0:5263");