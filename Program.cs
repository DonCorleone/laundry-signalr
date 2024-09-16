using LaundrySignalR.Hubs;
using LaundrySignalR.Services;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient",
        b =>
        {
            b.WithOrigins(["http://localhost:4200", "https://laundry-calendar.netlify.app"])
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddControllers();

var configuration = builder.Configuration;
var redisOptions = new ConfigurationOptions
{
    EndPoints = { "frankfurt-redis.render.com:6379" },
    Password = configuration["REDIS_CONNECTION_PWD"],
    User = configuration["REDIS_CONNECTION_USER"],
    Ssl = true,
    AbortOnConnectFail = false,
};

builder.Services.AddSingleton<IRedisService>(new RedisService(redisOptions));

builder.Services.AddSignalR().AddStackExchangeRedis(options =>
{
    options.Configuration = redisOptions;
});


var app = builder.Build();
app.UseRouting();
// Configure the HTTP request pipeline.
app.UseCors("AllowAngularClient");

app.MapControllers();
app.MapHub<ReservationHub>("/hub");

// Explicitly set the port to 5263
app.Run("http://0.0.0.0:5263");