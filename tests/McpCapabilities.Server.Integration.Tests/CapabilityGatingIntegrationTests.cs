using System.Text.Json.Nodes;

using McpCapabilities.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Integration.Tests;

public class CapabilityGatingIntegrationTests
{
  [McpServerToolType]
  public class AnnotatedTools
  {
    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Sampling, Message = "Needs LLM")]
    public string ToolRequiringSampling(string input) => input;

    [McpServerTool]
    public string ToolNoRequirements(string input) => input;

    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Elicitation)]
    public string ToolRequiringElicitation(string input) => input;
  }

  [Test]
  public async Task AddCapabilityGating_WithTools_SetsHandlersAndClearsCollection()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers();
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
    });

    services.AddMcpServer()
        .WithTools<AnnotatedTools>()
        .AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(options.Handlers.ListToolsHandler is not null).IsTrue();
    await Assert.That(options.ToolCollection is null).IsTrue();
  }

  [Test]
  public async Task AddCapabilityGating_WiresFilteringHandlers()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers();
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
    });

    services.AddMcpServer()
        .AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var listTools = options.Handlers.ListToolsHandler;
    var listPrompts = options.Handlers.ListPromptsHandler;
    var listResources = options.Handlers.ListResourcesHandler;
    await Assert.That(listTools is not null).IsTrue();
    await Assert.That(listPrompts is not null).IsTrue();
    await Assert.That(listResources is not null).IsTrue();
  }

  [Test]
  public async Task FilterByClientCapabilities_AllHidden_ReturnsCapabilityNotMetError()
  {
    var tool1 = new Tool
    {
      Name = "sampling_tool",
      Meta = CreateMeta(CapabilityFlag.Sampling),
    };
    var tool2 = new Tool
    {
      Name = "roots_tool",
      Meta = CreateMeta(CapabilityFlag.Roots),
    };

    var tools = new List<Tool> { tool1, tool2 };
    var clientCaps = new ClientCapabilities(); // no capabilities

    var result = tools.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsFailed).IsTrue();
    await Assert.That(result.Errors).Count().IsEqualTo(1);
    var error = result.Errors[0] as CapabilityNotMetError;
    await Assert.That(error).IsNotNull();
    await Assert.That(error!.Missing).IsNotEqualTo(CapabilityFlag.None);
    await Assert.That(error.PrimitiveName).IsEqualTo("tools/list");
  }

  private static JsonObject CreateMeta(CapabilityFlag flags)
  {
    var meta = new JsonObject();
    new ClientCapabilityRequirements { Required = flags }.WriteToMeta(meta);
    return meta;
  }
}