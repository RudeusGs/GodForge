using GodForge.Application;
using GodForge.Infrastructure;
using GodForge.Worker.Handlers;
using GodForge.Worker.Queues;

DotNetEnv.Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<RepositoryAnalysisPipelineHandler>();
builder.Services.AddHostedService<RabbitMqWorkerService>();

var host = builder.Build();
host.Run();
