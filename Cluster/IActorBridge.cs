using Akka.Actor;
using Akka.Cluster;
using Microsoft.AspNetCore.SignalR;

namespace DistChat.Cluster;

public interface IActorBridge
{
    ActorSystem GetActorSystem();
    Task<string> ResolveMemberHttpAddress(Member member);
    
    void CreateClient(HubCallerContext hubContext);
    Task DisconnectClient(HubCallerContext hubContext);
    Task SubscribeClient(HubCallerContext hubContext, string topic);
    void PublishMessage(HubCallerContext hubContext, string topic, string message);
}
