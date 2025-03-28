using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.DependencyInjection;
using Microsoft.AspNetCore.SignalR;

namespace DistChat.Cluster;

public class Client : ReceiveActor
{
    private readonly ILogger<Client> _logger;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly HubCallerContext _hubCallerContext;

    public record Subscribe(string Topic);

    public Client(ILogger<Client> logger, HubCallerContext hubCallerContext, IHubContext<ChatHub> hubContext)
    {
        _logger = logger;
        _hubCallerContext = hubCallerContext;
        _hubContext = hubContext;

        var pubSubMediator = DistributedPubSub.Get(Context.System).Mediator;

        Receive<Subscribe>(sub =>
        {
            pubSubMediator.Tell(new Akka.Cluster.Tools.PublishSubscribe.Subscribe(sub.Topic, Self));
        });
    
        ReceiveAsync<SubscribeAck>(async subAck =>
        {
            _logger.LogInformation("Client received suback {}", subAck.Subscribe.Topic);
        
            await _hubContext.Clients.Client(_hubCallerContext.ConnectionId)
                .SendAsync("ReceiveMessage", "system", $"Successfully subscribed to \"{subAck.Subscribe.Topic}\"");
        });

        ReceiveAsync<string>(async pub =>
        {
            _logger.LogInformation("Client received pub from cluster {}", pub);
            
            await _hubContext.Clients.Client(_hubCallerContext.ConnectionId)
                .SendAsync("ReceiveMessage", "system", pub);
        });
    }

    public static Props Props(ActorSystem system, HubCallerContext hubContext)
    {
        return DependencyResolver.For(system).Props<Client>(hubContext);
    }
    
    public static string FormatLocalPath(HubCallerContext context)
    {
        return $"/user/{FormatName(context)}";
    }
    
    public static string FormatName(HubCallerContext context)
    {
        return $"client-{context.ConnectionId}";
    }
}
