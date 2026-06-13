using McpCapabilities.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SampleMcpServer;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation
    {
        Name = "SampleMcpServer",
        Version = "1.0",
    };
    options.ServerInstructions =
        "This sample server demonstrates capability-gated MCP primitives. "
        + "Tools, prompts, and resources are hidden from clients that lack "
        + "the required capabilities (Sampling, Elicitation, Roots).";
})
    .WithCapabilityAwareTools<AiTools>()
    .WithPrompts<HelpfulPrompts>()
    .WithResources<WorkspaceResources>()
    .AddCapabilityGating()
    .WithStdioServerTransport();

var app = builder.Build();
await app.RunAsync();
