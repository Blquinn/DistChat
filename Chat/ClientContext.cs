using Microsoft.AspNetCore.SignalR;

namespace DistChat;

public class ClientContext
{
    public string ConnectionId { get; init; }
    public string? UserId { get; init; }

    public static ClientContext Create(HubCallerContext callerContext)
    {
        return new()
        {
            ConnectionId = callerContext.ConnectionId,
            UserId = callerContext.UserIdentifier,
        };
    }
}
