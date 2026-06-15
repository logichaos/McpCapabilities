using FakeItEasy;

using McpCapabilities.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class AddCapabilityGatingHandlerInvocationTests
{
  [McpServerToolType]
  private sealed class TestTools
  {
    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Sampling)]
    public string SamplingTool(string input) => input;

    [McpServerTool]
    public string UngatedTool(string input) => input;
  }

  [McpServerPromptType]
  private sealed class TestPrompts
  {
    [McpServerPrompt]
    [RequiredClientCapabilities(Required = CapabilityFlag.Elicitation)]
    public string ElicitationPrompt() => "elicitation";

    [McpServerPrompt]
    public string UngatedPrompt() => "greeting";
  }

  [McpServerResourceType]
  private sealed class TestResources
  {
    [McpServerResource]
    [RequiredClientCapabilities(Required = CapabilityFlag.Roots)]
    public string RootsResource() => "workspace";

    [McpServerResource]
    public string UngatedResource() => "app_info";
  }

  private static ServiceProvider BuildWithGating(
      McpServerHandlers? existingHandlers = null)
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = existingHandlers ?? new McpServerHandlers();
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
    });

    services.AddMcpServer()
        .WithCapabilityAwareTools<TestTools>()
        .WithPrompts<TestPrompts>()
        .WithResources<TestResources>()
        .AddCapabilityGating();

    return services.BuildServiceProvider();
  }

  private static RequestContext<TParams> CreateContext<TParams>(
      ClientCapabilities? capabilities,
      TParams parameters = default!)
      where TParams : class, new()
  {
    var server = A.Fake<McpServer>();
    var caps = capabilities;
    A.CallTo(() => server.ClientCapabilities).Returns(caps);

    var request = new JsonRpcRequest { Method = "test" };
    return new RequestContext<TParams>(server, request, parameters ?? new TParams());
  }

  [Test]
  public async Task Handler_FullCapabilityClient_SeesAllTools()
  {
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;
    var context = CreateContext<ListToolsRequestParams>(
        new ClientCapabilities
        {
          Sampling = new SamplingCapability(),
        });

    var result = await options.Handlers.ListToolsHandler!(context, default);
    var names = result.Tools.Select(t => t.Name).ToList();

    await Assert.That(names).IsEquivalentTo(["sampling_tool", "ungated_tool"]);
  }

  [Test]
  public async Task Handler_MinimalCapabilityClient_SeesOnlyUngatedTool()
  {
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;
    var context = CreateContext<ListToolsRequestParams>(new ClientCapabilities());

    var result = await options.Handlers.ListToolsHandler!(context, default);
    var names = result.Tools.Select(t => t.Name).ToList();

    await Assert.That(names).IsEquivalentTo(["ungated_tool"]);
    await Assert.That(names).DoesNotContain("sampling_tool");
  }

  [Test]
  public async Task Handler_FullCapabilityClient_SeesAllPrompts()
  {
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;
    var context = CreateContext<ListPromptsRequestParams>(
        new ClientCapabilities
        {
          Elicitation = new ElicitationCapability(),
        });

    var result = await options.Handlers.ListPromptsHandler!(context, default);
    var names = result.Prompts.Select(p => p.Name).ToList();

    await Assert.That(names).IsEquivalentTo(["elicitation_prompt", "ungated_prompt"]);
  }

  [Test]
  public async Task Handler_MinimalCapabilityClient_SeesOnlyUngatedPrompt()
  {
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;
    var context = CreateContext<ListPromptsRequestParams>(new ClientCapabilities());

    var result = await options.Handlers.ListPromptsHandler!(context, default);
    var names = result.Prompts.Select(p => p.Name).ToList();

    await Assert.That(names).IsEquivalentTo(["ungated_prompt"]);
    await Assert.That(names).DoesNotContain("elicitation_prompt");
  }

  [Test]
  public async Task Handler_FullCapabilityClient_SeesAllResources()
  {
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;
    var context = CreateContext<ListResourcesRequestParams>(
        new ClientCapabilities
        {
          Roots = new RootsCapability(),
        });

    var result = await options.Handlers.ListResourcesHandler!(context, default);
    var names = result.Resources.Select(r => r.Name).ToList();

    await Assert.That(names).IsEquivalentTo(["roots_resource", "ungated_resource"]);
  }

  [Test]
  public async Task Handler_MinimalCapabilityClient_SeesOnlyUngatedResource()
  {
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;
    var context = CreateContext<ListResourcesRequestParams>(new ClientCapabilities());

    var result = await options.Handlers.ListResourcesHandler!(context, default);
    var names = result.Resources.Select(r => r.Name).ToList();

    await Assert.That(names).IsEquivalentTo(["ungated_resource"]);
    await Assert.That(names).DoesNotContain("roots_resource");
  }

  [Test]
  public async Task Handler_FullCapabilityClient_SeesAllPrimitiveTypes()
  {
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var caps = new ClientCapabilities
    {
      Sampling = new SamplingCapability(),
      Roots = new RootsCapability(),
      Elicitation = new ElicitationCapability(),
    };

    var toolCtx = CreateContext<ListToolsRequestParams>(caps);
    var promptCtx = CreateContext<ListPromptsRequestParams>(caps);
    var resourceCtx = CreateContext<ListResourcesRequestParams>(caps);

    var tools = await options.Handlers.ListToolsHandler!(toolCtx, default);
    var prompts = await options.Handlers.ListPromptsHandler!(promptCtx, default);
    var resources = await options.Handlers.ListResourcesHandler!(resourceCtx, default);

    await Assert.That(tools.Tools).Count().IsEqualTo(2);
    await Assert.That(prompts.Prompts).Count().IsEqualTo(2);
    await Assert.That(resources.Resources).Count().IsEqualTo(2);
  }

  [Test]
  public async Task Handler_MinimalCapabilityClient_SeesOnlyUngatedAllTypes()
  {
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var toolCtx = CreateContext<ListToolsRequestParams>(new ClientCapabilities());
    var promptCtx = CreateContext<ListPromptsRequestParams>(new ClientCapabilities());
    var resourceCtx = CreateContext<ListResourcesRequestParams>(new ClientCapabilities());

    var tools = await options.Handlers.ListToolsHandler!(toolCtx, default);
    var prompts = await options.Handlers.ListPromptsHandler!(promptCtx, default);
    var resources = await options.Handlers.ListResourcesHandler!(resourceCtx, default);

    await Assert.That(tools.Tools).Count().IsEqualTo(1);
    await Assert.That(tools.Tools[0].Name).IsEqualTo("ungated_tool");
    await Assert.That(prompts.Prompts).Count().IsEqualTo(1);
    await Assert.That(prompts.Prompts[0].Name).IsEqualTo("ungated_prompt");
    await Assert.That(resources.Resources).Count().IsEqualTo(1);
    await Assert.That(resources.Resources[0].Name).IsEqualTo("ungated_resource");
  }

  [Test]
  public async Task Handler_NullClientCapabilities_OnlyUngatedPass()
  {
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var context = CreateContext<ListToolsRequestParams>(null);

    var result = await options.Handlers.ListToolsHandler!(context, default);
    var names = result.Tools.Select(t => t.Name).ToList();

    await Assert.That(names).IsEquivalentTo(["ungated_tool"]);
    await Assert.That(names).DoesNotContain("sampling_tool");
  }

  [Test]
  public async Task Handler_GatedPrimitive_HasMetaWritten()
  {
    // Verify that CaptureCapabilityRequirements ran during Configure
    // by checking that the gated tool is filtered out when no capabilities
    // are present. If Meta wasn't written, it would pass through unfiltered.
    var sp = BuildWithGating();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var context = CreateContext<ListToolsRequestParams>(new ClientCapabilities());

    var result = await options.Handlers.ListToolsHandler!(context, default);

    await Assert.That(result.Tools).Count().IsEqualTo(1);
    await Assert.That(result.Tools[0].Name).IsEqualTo("ungated_tool");
  }

  [Test]
  public async Task CombineHandlers_Tools_MergesResults()
  {
    var externalHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (_, _) => ValueTask.FromResult(new ListToolsResult
        {
          Tools = [new Tool { Name = "external_tool" }],
        }));

    var sp = BuildWithGating(existingHandlers: new McpServerHandlers
    {
      ListToolsHandler = externalHandler,
    });
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var context = CreateContext<ListToolsRequestParams>(
        new ClientCapabilities { Sampling = new SamplingCapability() });

    var result = await options.Handlers.ListToolsHandler!(context, default);
    var names = result.Tools.Select(t => t.Name).ToList();

    await Assert.That(names).Contains("external_tool");
    await Assert.That(names).Contains("sampling_tool");
    await Assert.That(names).Contains("ungated_tool");
    await Assert.That(names).Count().IsEqualTo(3);
  }

  [Test]
  public async Task CombineHandlers_Prompts_MergesResults()
  {
    var externalHandler = new McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>(
        (_, _) => ValueTask.FromResult(new ListPromptsResult
        {
          Prompts = [new Prompt { Name = "external_prompt" }],
        }));

    var sp = BuildWithGating(existingHandlers: new McpServerHandlers
    {
      ListPromptsHandler = externalHandler,
    });
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var context = CreateContext<ListPromptsRequestParams>(
        new ClientCapabilities { Elicitation = new ElicitationCapability() });

    var result = await options.Handlers.ListPromptsHandler!(context, default);
    var names = result.Prompts.Select(p => p.Name).ToList();

    await Assert.That(names).Contains("external_prompt");
    await Assert.That(names).Contains("elicitation_prompt");
    await Assert.That(names).Contains("ungated_prompt");
    await Assert.That(names).Count().IsEqualTo(3);
  }

  [Test]
  public async Task CombineHandlers_Resources_MergesResults()
  {
    var externalHandler = new McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>(
        (_, _) => ValueTask.FromResult(new ListResourcesResult
        {
          Resources = [new Resource { Name = "external_resource", Uri = "resource://ext" }],
        }));

    var sp = BuildWithGating(existingHandlers: new McpServerHandlers
    {
      ListResourcesHandler = externalHandler,
    });
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var context = CreateContext<ListResourcesRequestParams>(
        new ClientCapabilities { Roots = new RootsCapability() });

    var result = await options.Handlers.ListResourcesHandler!(context, default);
    var names = result.Resources.Select(r => r.Name).ToList();

    await Assert.That(names).Contains("external_resource");
    await Assert.That(names).Contains("roots_resource");
    await Assert.That(names).Contains("ungated_resource");
    await Assert.That(names).Count().IsEqualTo(3);
  }

  [Test]
  public async Task CombineHandlers_FilteringStillApplies()
  {
    var externalHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (_, _) => ValueTask.FromResult(new ListToolsResult
        {
          Tools = [new Tool { Name = "external_tool" }],
        }));

    var sp = BuildWithGating(existingHandlers: new McpServerHandlers
    {
      ListToolsHandler = externalHandler,
    });
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    // Minimal client — only ungated tools + external should pass
    var context = CreateContext<ListToolsRequestParams>(new ClientCapabilities());

    var result = await options.Handlers.ListToolsHandler!(context, default);
    var names = result.Tools.Select(t => t.Name).ToList();

    await Assert.That(names).Contains("external_tool");
    await Assert.That(names).Contains("ungated_tool");
    await Assert.That(names).DoesNotContain("sampling_tool");
    await Assert.That(names).Count().IsEqualTo(2);
  }
}