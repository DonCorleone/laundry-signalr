using LaundrySignalR.Hubs;

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
builder.Services.AddControllers();
// Configure SignalR to use Redis backplane
var redisPw = Environment.GetEnvironmentVariable("REDIS_CONNECTION_PWD");
Console.WriteLine(string.IsNullOrEmpty(redisPw)
    ? "Environment variable not found."
    : $"Environment variable value: {redisPw}");

var redisUser = Environment.GetEnvironmentVariable("REDIS_CONNECTION_USER");
Console.WriteLine(string.IsNullOrEmpty(redisUser)
    ? "Environment variable not found."
    : $"Environment variable value: {redisUser}");
builder.Services.AddSignalR().AddStackExchangeRedis(options =>
{
    options.Configuration = new StackExchange.Redis.ConfigurationOptions
    {
        EndPoints = { "frankfurt-redis.render.com:6379" },
        // use the password from render.com environment variables
        Password = redisPw,
        User = redisUser,
        Ssl = true,
        AbortOnConnectFail = false,
    };
});

var app = builder.Build();

app.UseRouting();
// Configure the HTTP request pipeline.
app.UseCors("AllowAngularClient");

app.MapControllers();
app.MapHub<ReservationHub>("/hub");

// Explicitly set the port to 5263
app.Run("http://0.0.0.0:5263");