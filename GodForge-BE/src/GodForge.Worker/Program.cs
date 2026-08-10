using GodForge.Infrastructure;
using GodForge.Infrastructure.Persistence;
using GodForge.Worker;

DotNetEnv.Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWorkerServices();

var host = builder.Build();
if (builder.Environment.IsDevelopment())
    await host.Services.InitializeGodForgeDatabaseAsync();

await host.RunAsync();
