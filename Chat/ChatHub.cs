using DistChat.Cluster;
using Microsoft.AspNetCore.SignalR;

namespace DistChat;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IActorBridge _actorBridge;

    public ChatHub(IActorBridge actorBridge, ILogger<ChatHub> logger)
    {
        _actorBridge = actorBridge;
        _logger = logger;
    }
    
    public async Task SendToRoom(string roomId, string user, string message)
    {
        _logger.LogInformation($"User sent message {user}: {message}");
        _actorBridge.PublishSend(ClientContext.Create(Context), roomId, "ReceiveMessage", new []{user, message});
    }
    
    public async Task SendDirectMessage(string user, string message)
    {
        _logger.LogInformation($"User sent message {user}: {message}");
        _actorBridge.PublishSend(ClientContext.Create(Context), $"dm/{user}", "ReceiveMessage", new []{user, message});
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("OnConnectedAsync {}", Context.ConnectionId);
        _actorBridge.CreateClient(Context);
        _actorBridge.SubscribeClient(Context, "messages");
        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("OnDisconnectedAsync {}", Context.ConnectionId);
        await _actorBridge.DisconnectClient(Context);
    }
}
