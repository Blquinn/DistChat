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

    public async Task SendMessage(string user, string message)
    {
        _logger.LogInformation($"User sent message {user}: {message}");
        _actorBridge.PublishMessage(Context, user, message);
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
