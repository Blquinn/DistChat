using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.DependencyInjection;
using Microsoft.AspNetCore.SignalR;

namespace DistChat.Cluster;

public class AkkaService : IHostedService, IActorBridge
{
    private readonly ILogger<AkkaService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IConfiguration _configuration;
    private readonly Config _nonSeedConfig;
    private readonly Config _seedConfig;
    private readonly IServiceProvider _serviceProvider;
    private ActorSystem _actorSystem;
    private IActorRef _pubSubMediator;
    
    public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime,
        IConfiguration configuration,
        [FromKeyedServices("seed")] Config seedConfig,
        [FromKeyedServices("non-seed")] Config nonSeedConfig, ILogger<AkkaService> logger)
    {
        _serviceProvider = serviceProvider;
        _applicationLifetime = appLifetime;
        _configuration = configuration;
        _seedConfig = seedConfig;
        _nonSeedConfig = nonSeedConfig;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var isSeed = _configuration.GetValue<bool>("Akka:SeedNode");
        
        var config = isSeed ? _seedConfig : _nonSeedConfig;

        var bootstrap = BootstrapSetup.Create().WithConfig(config)
            .WithConfigFallback(ClusterSingleton.DefaultConfig());

        // enable DI support inside this ActorSystem, if needed
        var diSetup = DependencyResolverSetup.Create(_serviceProvider);

        // merge this setup (and any others) together into ActorSystemSetup
        var actorSystemSetup = bootstrap.And(diSetup);

        // start ActorSystem
        _actorSystem = ActorSystem.Create("ClusterSystem", actorSystemSetup);

        // Distributed
        _pubSubMediator = DistributedPubSub.Get(_actorSystem).Mediator;
        
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        _actorSystem.WhenTerminated.ContinueWith(_ =>
        {
            _applicationLifetime.StopApplication();
        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // strictly speaking this may not be necessary - terminating the ActorSystem would also work
        // but this call guarantees that the shutdown of the cluster is graceful regardless
        await CoordinatedShutdown.Get(_actorSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
    }
    
    public void CreateClient(HubCallerContext callerContext)
    {
        _actorSystem.ActorOf(Client.Props(_actorSystem, callerContext), Client.FormatName(callerContext));
    }

    public async Task DisconnectClient(HubCallerContext hubContext)
    {
        var client = await _actorSystem.ActorSelection(Client.FormatLocalPath(hubContext))
            .ResolveOne(TimeSpan.FromSeconds(5));

        if (client == null)
        {
            return;
        }

        try
        {
            await client.GracefulStop(TimeSpan.FromSeconds(5));
            _logger.LogInformation("Stopped client actor for disconnect {}", client.Path);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to stop client actor for disconnect {}", client.Path);
        }
    }

    public async Task SubscribeClient(HubCallerContext hubContext, string topic)
    {
        var client = await _actorSystem.ActorSelection(Client.FormatLocalPath(hubContext))
            .ResolveOne(TimeSpan.FromSeconds(1));

        if (client == null)
        {
            return;
        }
        
        client.Tell(new Client.Subscribe(topic));
    }

    public void PublishMessage(HubCallerContext hubContext, string topic, string message)
    {
        _pubSubMediator.Tell(new Publish("messages", message));
    }
}