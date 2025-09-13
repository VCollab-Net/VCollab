using VCollab.Signaling;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<SignalingServer>();

var host = builder.Build();
host.Run();