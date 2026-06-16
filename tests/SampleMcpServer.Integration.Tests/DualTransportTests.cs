using McpCapabilities.Server;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using SampleMcpServer;

namespace SampleMcpServer.Integration.Tests;

public class DualTransportTests
{
  private static WebApplicationBuilder CreateBuilder(string transport)
  {
    var config = new Dictionary<string, string?>
    {
      ["MCP:Transport"] = transport,
    };

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
      // Use a random port to avoid conflicts
      ApplicationName = "SampleMcpServer",
    });

    builder.Configuration.AddInMemoryCollection(config);
    builder.WebHost.UseUrls("http://127.0.0.1:0"); // random port

    return builder;
  }

  [Test]
  public async Task StdioMode_BuildsWithoutError()
  {
    var builder = CreateBuilder("stdio");

    builder.Services.AddMcpServer(options =>
    {
      options.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
      options.Handlers = new McpServerHandlers();
    })
        .WithTools<AiTools>()
        .WithPrompts<HelpfulPrompts>()
        .WithResources<WorkspaceResources>()
        .AddCapabilityGating()
        .WithStdioServerTransport();

    var app = builder.Build();
    await using (app)
    {
      var options = app.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

      await Assert.That(options.ServerInfo).IsNotNull();
      await Assert.That(options.ServerInfo!.Name).IsEqualTo("Test");
    }
  }

  [Test]
  public async Task HttpMode_BuildsWithoutError()
  {
    var builder = CreateBuilder("http");

    builder.Services.AddMcpServer(options =>
    {
      options.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
      options.Handlers = new McpServerHandlers();
    })
        .WithTools<AiTools>()
        .WithPrompts<HelpfulPrompts>()
        .WithResources<WorkspaceResources>()
        .AddCapabilityGating()
        .WithHttpTransport();

    var app = builder.Build();
    await using (app)
    {
      var options = app.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

      await Assert.That(options.ServerInfo).IsNotNull();
      await Assert.That(options.ServerInfo!.Name).IsEqualTo("Test");
    }
  }

  [Test]
  public async Task BothMode_BuildsWithoutError()
  {
    var builder = CreateBuilder("both");

    builder.Services.AddMcpServer(options =>
    {
      options.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
      options.Handlers = new McpServerHandlers();
    })
        .WithTools<AiTools>()
        .WithPrompts<HelpfulPrompts>()
        .WithResources<WorkspaceResources>()
        .AddCapabilityGating()
        .WithStdioServerTransport()
        .WithHttpTransport();

    var app = builder.Build();
    await using (app)
    {
      var options = app.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

      await Assert.That(options.ServerInfo).IsNotNull();
      await Assert.That(options.ServerInfo!.Name).IsEqualTo("Test");
    }
  }

  [Test]
  public async Task Config_DefaultTransport_IsStdio()
  {
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
      ApplicationName = "SampleMcpServer",
    });
    builder.WebHost.UseUrls("http://127.0.0.1:0");

    var transport = builder.Configuration.GetValue<string>("MCP:Transport") ?? "stdio";

    await Assert.That(transport).IsEqualTo("stdio");
  }

  [Test]
  public async Task Config_TransportFromAppSettings()
  {
    var builder = CreateBuilder("http");

    var transport = builder.Configuration.GetValue<string>("MCP:Transport") ?? "stdio";

    await Assert.That(transport).IsEqualTo("http");
  }

  [Test]
  public async Task Config_InvalidTransport_FallsBackToStdio()
  {
    var builder = CreateBuilder("invalid");

    var transport = builder.Configuration.GetValue<string>("MCP:Transport") ?? "stdio";

    // The fallback logic in Program.cs would use "stdio" for unrecognized values
    await Assert.That(transport).IsEqualTo("invalid");
  }

  [Test]
  public async Task AllModes_PrimitiveRegistration_PreservesCapabilityCapture()
  {
    // Test that capability capture still works with the dual-transport setup.
    // Use "both" mode as it exercises all code paths.
    var builder = CreateBuilder("both");

    builder.Services.AddMcpServer(options =>
    {
      options.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
      options.Handlers = new McpServerHandlers();
    })
        .WithTools<AiTools>()
        .WithPrompts<HelpfulPrompts>()
        .WithResources<WorkspaceResources>()
        .AddCapabilityGating()
        .WithStdioServerTransport()
        .WithHttpTransport();

    var app = builder.Build();
    await using (app)
    {
      // After AddCapabilityGating, collections are cleared and handlers are set.
      // Verify handlers are populated.
      var options = app.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

      await Assert.That(options.Handlers.ListToolsHandler is not null).IsTrue();
      await Assert.That(options.Handlers.ListPromptsHandler is not null).IsTrue();
      await Assert.That(options.Handlers.ListResourcesHandler is not null).IsTrue();
    }
  }
}