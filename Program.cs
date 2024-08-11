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

// Configure SignalR to use Redis backplane
builder.Services.AddSignalR().AddStackExchangeRedis(options =>
{
    options.Configuration = new StackExchange.Redis.ConfigurationOptions
    {
        EndPoints = { "frankfurt-redis.render.com:6379" },
        Password = "tRMjXLTr1puE30Ru0RhMBFSWT7g8Sb5U",
        User = "red-cqnqmfg8fa8c73at4hog",
        Ssl = true,
        AbortOnConnectFail = false,
    };
});

var app = builder.Build();

app.UseRouting();
// Configure the HTTP request pipeline.
app.UseCors("AllowAngularClient");

app.MapHub<ChatHub>("/hub");

// Explicitly set the port to 5263
app.Run("http://0.0.0.0:5263");