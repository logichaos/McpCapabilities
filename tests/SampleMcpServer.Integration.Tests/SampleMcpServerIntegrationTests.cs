using System.Text.Json.Nodes;

using McpCapabilities.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using SampleMcpServer;

namespace SampleMcpServer.Integration.Tests;

public class SampleMcpServerIntegrationTests
{
  private static IServiceProvider BuildServer(bool withGating = true)
  {
    var services = new ServiceCollection();
    services.AddOptions();

    var builder = services.AddMcpServer(options =>
    {
      options.ServerInfo = new Implementation { Name = "SampleMcpServer", Version = "1.0" };
      options.Handlers = new McpServerHandlers();
    })
        .WithCapabilityAwareTools<AiTools>()
        .WithPrompts<HelpfulPrompts>()
        .WithResources<WorkspaceResources>();

    services.AddSingleton<IConfigureOptions<McpServerOptions>>(
        new CaptureAllPrimitives());

    if (withGating)
      builder.AddCapabilityGating();

    return services.BuildServiceProvider();
  }

  private sealed class CaptureAllPrimitives : IConfigureOptions<McpServerOptions>
  {
    public void Configure(McpServerOptions options)
    {
      if (options.ToolCollection is not null)
      {
        foreach (var tool in options.ToolCollection)
          tool.CaptureCapabilityRequirements();
      }
      if (options.PromptCollection is not null)
      {
        foreach (var prompt in options.PromptCollection)
          prompt.CaptureCapabilityRequirements();
      }
      if (options.ResourceCollection is not null)
      {
        foreach (var resource in options.ResourceCollection)
          resource.CaptureCapabilityRequirements();
      }
    }
  }

  [Test]
  public async Task WithoutGating_CollectionsArePopulated()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(options.ToolCollection).IsNotNull();
    await Assert.That(options.ToolCollection!).Count().IsEqualTo(4);
    await Assert.That(options.PromptCollection).IsNotNull();
    await Assert.That(options.PromptCollection!).Count().IsEqualTo(2);
    await Assert.That(options.ResourceCollection).IsNotNull();
    await Assert.That(options.ResourceCollection!).Count().IsEqualTo(2);
  }

  [Test]
  public async Task WithoutGating_ToolCapabilitiesAreCaptured()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var aiSummarize = options.ToolCollection!.FirstOrDefault(t =>
        t.ProtocolTool.Name == "ai_summarize");
    await Assert.That(aiSummarize).IsNotNull();
    var reqs = aiSummarize!.GetCapabilityRequirements();
    await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Sampling);
  }

  [Test]
  public async Task WithoutGating_PromptCapabilitiesAreCaptured()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var confirmAction = options.PromptCollection!.FirstOrDefault(p =>
        p.ProtocolPrompt.Name == "confirm_action");
    await Assert.That(confirmAction).IsNotNull();
    var reqs = confirmAction!.GetCapabilityRequirements();
    await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Elicitation);
  }

  [Test]
  public async Task WithoutGating_ResourceCapabilitiesAreCaptured()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var workspaceFiles = options.ResourceCollection!.FirstOrDefault(r =>
        r.ProtocolResource?.Name == "workspace_files");
    await Assert.That(workspaceFiles).IsNotNull();
    var reqs = workspaceFiles!.GetCapabilityRequirements();
    await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Roots);
  }

  [Test]
  public async Task WithoutGating_UngatedToolHasNoCapabilityRequirements()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var echo = options.ToolCollection!.FirstOrDefault(t =>
        t.ProtocolTool.Name == "echo");
    await Assert.That(echo).IsNotNull();
    var reqs = echo!.GetCapabilityRequirements();
    await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.None);
  }

  [Test]
  public async Task FilterByClientCapabilities_Tools_FullClientSeesAll()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var protools = options.ToolCollection!
        .Select(t => t.ProtocolTool)
        .ToList();
    var clientCaps = new ClientCapabilities
    {
      Sampling = new SamplingCapability(),
      Elicitation = new ElicitationCapability(),
    };

    var result = protools.FilterByClientCapabilities(clientCaps);
    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(4);
  }

  [Test]
  public async Task FilterByClientCapabilities_Tools_MinimalClientSeesOnlyEcho()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var protools = options.ToolCollection!
        .Select(t => t.ProtocolTool)
        .ToList();
    var clientCaps = new ClientCapabilities();

    var result = protools.FilterByClientCapabilities(clientCaps);
    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(1);
    await Assert.That(result.Value[0].Name).IsEqualTo("echo");
  }

  [Test]
  public async Task FilterByClientCapabilities_Prompts_FullClientSeesBoth()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var proPrompts = options.PromptCollection!
        .Select(p => p.ProtocolPrompt)
        .ToList();
    var clientCaps = new ClientCapabilities
    {
      Elicitation = new ElicitationCapability(),
    };

    var result = proPrompts.FilterByClientCapabilities(clientCaps);
    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(2);
  }

  [Test]
  public async Task FilterByClientCapabilities_Prompts_MinimalClientSeesOnlyGreeting()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var proPrompts = options.PromptCollection!
        .Select(p => p.ProtocolPrompt)
        .ToList();
    var clientCaps = new ClientCapabilities();

    var result = proPrompts.FilterByClientCapabilities(clientCaps);
    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(1);
    await Assert.That(result.Value[0].Name).IsEqualTo("greeting");
  }

  [Test]
  public async Task FilterByClientCapabilities_Resources_FullClientSeesBoth()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var proResources = options.ResourceCollection!
        .Select(r => r.ProtocolResource)
        .OfType<Resource>()
        .ToList();
    var clientCaps = new ClientCapabilities
    {
      Roots = new RootsCapability(),
    };

    var result = proResources.FilterByClientCapabilities(clientCaps);
    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(2);
  }

  [Test]
  public async Task FilterByClientCapabilities_Resources_MinimalClientSeesOnlyAppInfo()
  {
    var sp = BuildServer(withGating: false);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var proResources = options.ResourceCollection!
        .Select(r => r.ProtocolResource)
        .OfType<Resource>()
        .ToList();
    var clientCaps = new ClientCapabilities();

    var result = proResources.FilterByClientCapabilities(clientCaps);
    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(1);
    await Assert.That(result.Value[0].Name).IsEqualTo("app_info");
  }

  [Test]
  public async Task WithGating_CollectionsAreCleared()
  {
    var sp = BuildServer(withGating: true);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(options.ToolCollection).IsNull();
    await Assert.That(options.PromptCollection).IsNull();
    await Assert.That(options.ResourceCollection).IsNull();
  }

  [Test]
  public async Task WithGating_ListHandlersAreSet()
  {
    var sp = BuildServer(withGating: true);
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(options.Handlers.ListToolsHandler is not null).IsTrue();
    await Assert.That(options.Handlers.ListPromptsHandler is not null).IsTrue();
    await Assert.That(options.Handlers.ListResourcesHandler is not null).IsTrue();
  }
}