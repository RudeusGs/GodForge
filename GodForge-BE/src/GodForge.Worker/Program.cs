using GodForge.Worker;

// Milestone 1 Placeholder: This worker host currently serves as a structural baseline.
// Actual logical workers (Clone, Parser, Analyzer) will be implemented in future milestones.

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
