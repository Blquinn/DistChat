using Akka.Configuration;
using DistChat;
using DistChat.Cluster;

var builder = WebApplication.CreateBuilder(args);

var seedNodeConf = ConfigurationFactory.ParseString(File.ReadAllText("./Properties/Akka/seed-node.conf"))!;
var nonSeedNodeConf = ConfigurationFactory.ParseString(File.ReadAllText("./Properties/Akka/non-seed-node.conf"))!;

builder.Services.AddKeyedSingleton<Config>("seed", seedNodeConf);
builder.Services.AddKeyedSingleton<Config>("non-seed", nonSeedNodeConf);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddRouting();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IActorBridge, AkkaService>();

// starts the IHostedService, which creates the ActorSystem and actors
builder.Services.AddHostedService<AkkaService>(sp => (AkkaService)sp.GetRequiredService<IActorBridge>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(cors => cors
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()
        .SetIsOriginAllowed(origin => true));
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();

app.UseAuthorization();

app.MapRazorPages();
app.MapHub<ChatHub>("/chat");

app.Run();