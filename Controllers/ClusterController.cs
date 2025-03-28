using DistChat.Cluster;
using Microsoft.AspNetCore.Mvc;

namespace DistChat.Controllers;

[ApiController]
[Route("cluster")]
public class ClusterController : ControllerBase
{
    private readonly IActorBridge _actorBridge;

    public ClusterController(IActorBridge actorBridge)
    {
        _actorBridge = actorBridge;
    }

    [HttpGet("list-hosts")]
    public async Task<List<string>> ListClusterHosts()
    {
        var cluster = Akka.Cluster.Cluster.Get(_actorBridge.GetActorSystem());
        var addresses = new List<string>();
        foreach (var member in cluster.State.Members)
        {
            var addr = await _actorBridge.ResolveMemberHttpAddress(member);
            addresses.Add(addr);
        }
        
        return addresses;
    }
}
