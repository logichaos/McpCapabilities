using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol.Protocol;

using SampleMcpServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetValue<string>("Cors:AllowedOrigins") ?? "https://localhost:7197")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Mcp-Session-Id");
    });
});

var transport = builder.Configuration.GetValue<string>("MCP:Transport") ?? "stdio";
var isHttp = transport is "http" or "both";
var isStdio = transport is "stdio" or "both";

var mcpBuilder = builder.Services.AddMcpServer(options =>
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
    .AddCapabilityGating();
if (isStdio)
  mcpBuilder.WithStdioServerTransport();

if (isHttp)
  mcpBuilder.WithHttpTransport();

var app = builder.Build();
var env = app.Environment.EnvironmentName;

if (isHttp)
{
  app.UseCors();
  app.MapMcp();
}

await app.RunAsync();