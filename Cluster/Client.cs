using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.DependencyInjection;
using Microsoft.AspNetCore.SignalR;

namespace DistChat.Cluster;

// The Client actor is created per client connection.
// It routes messages to the signalr socket.
public class Client : ReceiveActor
{
    private readonly ILogger<Client> _logger;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly HubCallerContext _hubCallerContext;

    public record Subscribe(string Topic);
    public record SignalrSend(ClientContext Context, string Method, object[] Args);

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
       
        ReceiveAsync<SignalrSend>(async send =>
        {
            _logger.LogInformation("Client received signalr send from cluster {}", send);
            
            using var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(5));
            
            await _hubContext.Clients.Client(_hubCallerContext.ConnectionId)
                .SendCoreAsync(send.Method, send.Args, tokenSource.Token);
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
