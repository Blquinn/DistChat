using Akka.Actor;
using Akka.Cluster;
using Akka.DependencyInjection;

namespace DistChat.Cluster;

public class NodeInfoResolver : ReceiveActor
{
    private readonly IConfiguration _configuration;
    public record ResolveHttpAddress();
    public record ResolveHttpAddressResponse(string Url);
    
    public NodeInfoResolver(IConfiguration configuration)
    {
        _configuration = configuration;
        Receive<ResolveHttpAddress>(r =>
        {
            Sender.Tell(new ResolveHttpAddressResponse(_configuration.GetValue<string>("ASPNETCORE_URLS")!));
        });
    }

    public static Props Props(ActorSystem system)
    {
        return DependencyResolver.For(system).Props<NodeInfoResolver>();
    }
    
    public static string GetName()
    {
        return "node-info-resolver";
    }
    
    public static string GetLocalPath()
    {
        return $"/user/{GetName()}";
    }
    
    public static string GetClusterAddress(Member member)
    {
        return $"{member.Address}/{GetLocalPath()}";
    }
}
