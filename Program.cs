
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
builder.Services.AddSignalR();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
// Configure the HTTP request pipeline.
app.UseCors("AllowAngularClient");

app.MapHub<ChatHub>("/hub");

// Explicitly set the port to 5263
app.Run("http://0.0.0.0:5263");