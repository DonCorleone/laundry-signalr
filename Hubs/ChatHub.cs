using Microsoft.AspNetCore.SignalR;

namespace LaundrySignalR.Hubs;

public class ChatHub(ILogger<ChatHub> logger) : Hub
{
    public async Task NewMessage(string username, string message)
    {
        try
        {
            logger.LogInformation("New message from {Username}: {Message}", username, message);
            await Clients.All.SendAsync("messageReceived", username, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in NewMessage");
            throw;
        }
    }
}