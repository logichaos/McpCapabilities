using McpCapabilities.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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

builder.Services.AddSingleton<IConfigureOptions<McpServerOptions>>(
    new PromptResourceCapabilityCapture());

var app = builder.Build();
await app.RunAsync();

public sealed class PromptResourceCapabilityCapture : IConfigureOptions<McpServerOptions>
{
    public void Configure(McpServerOptions options)
    {
        if (options.PromptCollection is not null)
        {
            foreach (var prompt in options.PromptCollection)
            {
                prompt.CaptureCapabilityRequirements();
            }
        }

        if (options.ResourceCollection is not null)
        {
            foreach (var resource in options.ResourceCollection)
            {
                resource.CaptureCapabilityRequirements();
            }
        }
    }
}
