using ZakYip.Singulation.Host;
using ZakYip.Singulation.Host.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<SingulationWorker>();

var host = builder.Build();
host.Run();