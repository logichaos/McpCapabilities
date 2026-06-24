using FakeItEasy;

using McpCapabilities.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class MultiTenantCapabilityGatingTests
{
  [McpServerToolType]
  private sealed class TenantTools
  {
    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Sampling)]
    public string SamplingTool() => "sampling";

    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Elicitation)]
    public string ElicitationTool() => "elicitation";

    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Roots)]
    public string RootsTool() => "roots";

    [McpServerTool]
    public string UngatedTool() => "ungated";
  }

  [McpServerPromptType]
  private sealed class TenantPrompts
  {
    [McpServerPrompt]
    [RequiredClientCapabilities(Required = CapabilityFlag.Sampling)]
    public string SamplingPrompt() => "sampling";

    [McpServerPrompt]
    [RequiredClientCapabilities(Required = CapabilityFlag.Elicitation)]
    public string ElicitationPrompt() => "elicitation";

    [McpServerPrompt]
    public string UngatedPrompt() => "ungated";
  }

  [McpServerResourceType]
  private sealed class TenantResources
  {
    [McpServerResource]
    [RequiredClientCapabilities(Required = CapabilityFlag.Roots)]
    public string RootsResource() => "roots";

    [McpServerResource]
    public string UngatedResource() => "ungated";
  }

  private static McpServerOptions BuildServerOptions()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers();
      opt.ServerInfo = new Implementation { Name = "MultiTenantServer", Version = "1.0" };
    });

    services.AddMcpServer()
        .WithTools<TenantTools>()
        .WithPrompts<TenantPrompts>()
        .WithResources<TenantResources>()
        .AddCapabilityGating();

    return services.BuildServiceProvider().GetRequiredService<IOptions<McpServerOptions>>().Value;
  }

  private static RequestContext<TParams> CreateContext<TParams>(
      ClientCapabilities? capabilities,
      TParams? parameters = null)
      where TParams : class, new()
  {
    var server = A.Fake<McpServer>();
    A.CallTo(() => server.ClientCapabilities).Returns(capabilities);
    return new RequestContext<TParams>(server, new JsonRpcRequest { Method = "test" }, parameters ?? new TParams());
  }

  private static RequestContext<CallToolRequestParams> CreateCallToolContext(
      ClientCapabilities? capabilities,
      CallToolRequestParams parameters)
  {
    var server = A.Fake<McpServer>();
    A.CallTo(() => server.ClientCapabilities).Returns(capabilities);
    return new RequestContext<CallToolRequestParams>(server, new JsonRpcRequest { Method = "test" }, parameters);
  }

  private static ClientCapabilities FullCapabilities => new()
  {
    Sampling = new SamplingCapability(),
    Elicitation = new ElicitationCapability(),
    Roots = new RootsCapability(),
  };

  private static ClientCapabilities SamplingOnlyCapabilities => new()
  {
    Sampling = new SamplingCapability(),
  };

  private static ClientCapabilities NoCapabilities => new();

  [Test]
  public async Task MultiTenant_ThreeClients_EachSeesCorrectToolSet()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.ListToolsHandler!;

    var fullResult = await handler(CreateContext<ListToolsRequestParams>(FullCapabilities), default);
    var samplingResult = await handler(CreateContext<ListToolsRequestParams>(SamplingOnlyCapabilities), default);
    var noneResult = await handler(CreateContext<ListToolsRequestParams>(NoCapabilities), default);

    await Assert.That(fullResult.Tools.Select(t => t.Name))
        .IsEquivalentTo(["sampling_tool", "elicitation_tool", "roots_tool", "ungated_tool"]);
    await Assert.That(samplingResult.Tools.Select(t => t.Name))
        .IsEquivalentTo(["sampling_tool", "ungated_tool"]);
    await Assert.That(noneResult.Tools.Select(t => t.Name))
        .IsEquivalentTo(["ungated_tool"]);
  }

  [Test]
  public async Task MultiTenant_ThreeClients_EachSeesCorrectPromptSet()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.ListPromptsHandler!;

    var fullResult = await handler(CreateContext<ListPromptsRequestParams>(FullCapabilities), default);
    var samplingResult = await handler(CreateContext<ListPromptsRequestParams>(SamplingOnlyCapabilities), default);
    var noneResult = await handler(CreateContext<ListPromptsRequestParams>(NoCapabilities), default);

    await Assert.That(fullResult.Prompts.Select(p => p.Name))
        .IsEquivalentTo(["sampling_prompt", "elicitation_prompt", "ungated_prompt"]);
    await Assert.That(samplingResult.Prompts.Select(p => p.Name))
        .IsEquivalentTo(["sampling_prompt", "ungated_prompt"]);
    await Assert.That(noneResult.Prompts.Select(p => p.Name))
        .IsEquivalentTo(["ungated_prompt"]);
  }

  [Test]
  public async Task MultiTenant_TwoClients_EachSeesCorrectResourceSet()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.ListResourcesHandler!;

    var fullResult = await handler(CreateContext<ListResourcesRequestParams>(FullCapabilities), default);
    var noneResult = await handler(CreateContext<ListResourcesRequestParams>(NoCapabilities), default);

    await Assert.That(fullResult.Resources.Select(r => r.Name))
        .IsEquivalentTo(["roots_resource", "ungated_resource"]);
    await Assert.That(noneResult.Resources.Select(r => r.Name))
        .IsEquivalentTo(["ungated_resource"]);
  }

  [Test]
  public async Task MultiTenant_InterleavedListRequests_ResultsNeverBleedBetweenClients()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.ListToolsHandler!;

    for (var round = 0; round < 4; round++)
    {
      var fullResult = await handler(CreateContext<ListToolsRequestParams>(FullCapabilities), default);
      var samplingResult = await handler(CreateContext<ListToolsRequestParams>(SamplingOnlyCapabilities), default);
      var noneResult = await handler(CreateContext<ListToolsRequestParams>(NoCapabilities), default);

      await Assert.That(fullResult.Tools).Count().IsEqualTo(4);
      await Assert.That(samplingResult.Tools.Select(t => t.Name))
          .IsEquivalentTo(["sampling_tool", "ungated_tool"]);
      await Assert.That(noneResult.Tools.Select(t => t.Name))
          .IsEquivalentTo(["ungated_tool"]);
    }
  }

  [Test]
  public async Task MultiTenant_InterleavedAllPrimitiveTypes_EachClientAlwaysSeesItsOwnView()
  {
    var options = BuildServerOptions();

    for (var round = 0; round < 3; round++)
    {
      var fullTools = await options.Handlers.ListToolsHandler!(
          CreateContext<ListToolsRequestParams>(FullCapabilities), default);
      var noneTools = await options.Handlers.ListToolsHandler!(
          CreateContext<ListToolsRequestParams>(NoCapabilities), default);

      var fullPrompts = await options.Handlers.ListPromptsHandler!(
          CreateContext<ListPromptsRequestParams>(FullCapabilities), default);
      var nonePrompts = await options.Handlers.ListPromptsHandler!(
          CreateContext<ListPromptsRequestParams>(NoCapabilities), default);

      var fullResources = await options.Handlers.ListResourcesHandler!(
          CreateContext<ListResourcesRequestParams>(FullCapabilities), default);
      var noneResources = await options.Handlers.ListResourcesHandler!(
          CreateContext<ListResourcesRequestParams>(NoCapabilities), default);

      await Assert.That(fullTools.Tools).Count().IsEqualTo(4);
      await Assert.That(noneTools.Tools).Count().IsEqualTo(1);
      await Assert.That(fullPrompts.Prompts).Count().IsEqualTo(3);
      await Assert.That(nonePrompts.Prompts).Count().IsEqualTo(1);
      await Assert.That(fullResources.Resources).Count().IsEqualTo(2);
      await Assert.That(noneResources.Resources).Count().IsEqualTo(1);
    }
  }

  [Test]
  public async Task MultiTenant_MultipleRounds_SameClientAlwaysSeesConsistentResults()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.ListToolsHandler!;

    for (var round = 0; round < 5; round++)
    {
      var result = await handler(CreateContext<ListToolsRequestParams>(SamplingOnlyCapabilities), default);
      await Assert.That(result.Tools.Select(t => t.Name))
          .IsEquivalentTo(["sampling_tool", "ungated_tool"]);
    }
  }

  [Test]
  public async Task MultiTenant_MultipleRoundsInterleaved_AllClientsAlwaysSeeCorrectResults()
  {
    var options = BuildServerOptions();
    var toolHandler = options.Handlers.ListToolsHandler!;
    var promptHandler = options.Handlers.ListPromptsHandler!;

    for (var round = 0; round < 5; round++)
    {
      if (round % 2 == 0)
      {
        var r = await toolHandler(CreateContext<ListToolsRequestParams>(NoCapabilities), default);
        await Assert.That(r.Tools).Count().IsEqualTo(1);
        r = await toolHandler(CreateContext<ListToolsRequestParams>(FullCapabilities), default);
        await Assert.That(r.Tools).Count().IsEqualTo(4);
        r = await toolHandler(CreateContext<ListToolsRequestParams>(SamplingOnlyCapabilities), default);
        await Assert.That(r.Tools).Count().IsEqualTo(2);
      }
      else
      {
        var r = await toolHandler(CreateContext<ListToolsRequestParams>(FullCapabilities), default);
        await Assert.That(r.Tools).Count().IsEqualTo(4);
        r = await toolHandler(CreateContext<ListToolsRequestParams>(SamplingOnlyCapabilities), default);
        await Assert.That(r.Tools).Count().IsEqualTo(2);
        r = await toolHandler(CreateContext<ListToolsRequestParams>(NoCapabilities), default);
        await Assert.That(r.Tools).Count().IsEqualTo(1);
      }

      var samplingPrompts = await promptHandler(CreateContext<ListPromptsRequestParams>(SamplingOnlyCapabilities), default);
      await Assert.That(samplingPrompts.Prompts.Select(p => p.Name))
          .IsEquivalentTo(["sampling_prompt", "ungated_prompt"]);
    }
  }

  [Test]
  public async Task MultiTenant_ClientCapabilityUpgradeBetweenSessions_SeesExpandedToolSet()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.ListToolsHandler!;

    var session1 = await handler(CreateContext<ListToolsRequestParams>(NoCapabilities), default);
    var session2 = await handler(CreateContext<ListToolsRequestParams>(SamplingOnlyCapabilities), default);
    var session3 = await handler(CreateContext<ListToolsRequestParams>(FullCapabilities), default);

    await Assert.That(session1.Tools.Select(t => t.Name)).IsEquivalentTo(["ungated_tool"]);
    await Assert.That(session2.Tools.Select(t => t.Name))
        .IsEquivalentTo(["sampling_tool", "ungated_tool"]);
    await Assert.That(session3.Tools).Count().IsEqualTo(4);
  }

  [Test]
  public async Task MultiTenant_ClientCapabilityDowngradeBetweenSessions_SeesReducedToolSet()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.ListToolsHandler!;

    var session1 = await handler(CreateContext<ListToolsRequestParams>(FullCapabilities), default);
    var session2 = await handler(CreateContext<ListToolsRequestParams>(SamplingOnlyCapabilities), default);
    var session3 = await handler(CreateContext<ListToolsRequestParams>(NoCapabilities), default);

    await Assert.That(session1.Tools).Count().IsEqualTo(4);
    await Assert.That(session2.Tools).Count().IsEqualTo(2);
    await Assert.That(session2.Tools.Select(t => t.Name)).DoesNotContain("elicitation_tool");
    await Assert.That(session2.Tools.Select(t => t.Name)).DoesNotContain("roots_tool");
    await Assert.That(session3.Tools.Select(t => t.Name)).IsEquivalentTo(["ungated_tool"]);
  }

  [Test]
  public async Task MultiTenant_CallTool_GatingIsIsolatedPerClient()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.CallToolHandler!;

    for (var round = 0; round < 3; round++)
    {
      var allowedBlocked = false;
      try
      {
        await handler(
            CreateCallToolContext(
                new ClientCapabilities { Sampling = new SamplingCapability() },
                new CallToolRequestParams { Name = "sampling_tool" }),
            default);
      }
      catch (McpProtocolException e) when (e.Message.StartsWith("Client missing"))
      {
        allowedBlocked = true;
      }
      catch { /* invocation may fail against fake server */ }

      await Assert.That(allowedBlocked).IsFalse();

      await Assert.That(async () =>
      {
        await handler(
            CreateCallToolContext(
                new ClientCapabilities(),
                new CallToolRequestParams { Name = "sampling_tool" }),
            default);
      }).Throws<McpProtocolException>();
    }
  }

  [Test]
  public async Task MultiTenant_CallTool_NoCapClientCallingUngatedDoesNotBlockCapableClientOnGatedTool()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.CallToolHandler!;

    var ungatedBlocked = false;
    try
    {
      await handler(
          CreateCallToolContext(NoCapabilities, new CallToolRequestParams { Name = "ungated_tool" }),
          default);
    }
    catch (McpProtocolException e) when (e.Message.StartsWith("Client missing"))
    {
      ungatedBlocked = true;
    }
    catch { /* invocation may fail against fake server */ }

    await Assert.That(ungatedBlocked).IsFalse();

    var capableBlocked = false;
    try
    {
      await handler(
          CreateCallToolContext(
              new ClientCapabilities { Sampling = new SamplingCapability() },
              new CallToolRequestParams { Name = "sampling_tool" }),
          default);
    }
    catch (McpProtocolException e) when (e.Message.StartsWith("Client missing"))
    {
      capableBlocked = true;
    }
    catch { /* invocation may fail against fake server */ }

    await Assert.That(capableBlocked).IsFalse();
  }

  [Test]
  public async Task MultiTenant_ConcurrentListRequests_AllClientsGetCorrectResults()
  {
    var options = BuildServerOptions();
    var handler = options.Handlers.ListToolsHandler!;

    var tasks = Enumerable.Range(0, 12).Select(i =>
    {
      ClientCapabilities caps = (i % 3) switch
      {
        0 => FullCapabilities,
        1 => SamplingOnlyCapabilities,
        _ => NoCapabilities,
      };
      return handler(CreateContext<ListToolsRequestParams>(caps), default).AsTask();
    }).ToList();

    var results = await Task.WhenAll(tasks);

    for (var i = 0; i < results.Length; i++)
    {
      var expected = (i % 3) switch
      {
        0 => 4,
        1 => 2,
        _ => 1,
      };
      await Assert.That(results[i].Tools).Count().IsEqualTo(expected);
    }
  }
}
