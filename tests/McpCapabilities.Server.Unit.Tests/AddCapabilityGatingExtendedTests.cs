using System.Reflection;
using System.Text.Json.Nodes;

using McpCapabilities.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class AddCapabilityGatingExtendedTests
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

  private static void ConfigureOptions(McpServerOptions opt)
  {
    opt.Handlers = new McpServerHandlers();
    opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
  }

  [Test]
  public async Task Configure_WithPopulatedToolCollection_ClearsCollection()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(ConfigureOptions);

    services.AddMcpServer()
        .WithCapabilityAwareTools<TestTools>()
        .AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(options.ToolCollection is null).IsTrue();
  }

  [Test]
  public async Task Configure_WithPopulatedPromptCollection_ClearsCollection()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(ConfigureOptions);

    services.AddMcpServer()
        .WithPrompts<TestPrompts>()
        .AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(options.PromptCollection is null).IsTrue();
  }

  [Test]
  public async Task Configure_WithPopulatedResourceCollection_ClearsCollection()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(ConfigureOptions);

    services.AddMcpServer()
        .WithResources<TestResources>()
        .AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(options.ResourceCollection is null).IsTrue();
  }

  [Test]
  public async Task Configure_WithAllPrimitiveTypes_SetsAllHandlers()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(ConfigureOptions);

    services.AddMcpServer()
        .WithCapabilityAwareTools<TestTools>()
        .WithPrompts<TestPrompts>()
        .WithResources<TestResources>()
        .AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(options.Handlers.ListToolsHandler is not null).IsTrue();
    await Assert.That(options.Handlers.ListPromptsHandler is not null).IsTrue();
    await Assert.That(options.Handlers.ListResourcesHandler is not null).IsTrue();
    await Assert.That(options.ToolCollection is null).IsTrue();
    await Assert.That(options.PromptCollection is null).IsTrue();
    await Assert.That(options.ResourceCollection is null).IsTrue();
  }

  [Test]
  public async Task Configure_WrapsExistingListPromptsHandler()
  {
    var services = new ServiceCollection();
    services.AddOptions();

    var existingHandler = new McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>(
        (_, _) => ValueTask.FromResult(new ListPromptsResult { Prompts = [] }));

    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers
      {
        ListPromptsHandler = existingHandler,
      };
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
    });

    var builder = new TestMcpServerBuilder(services);
    builder.AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var resolvedOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(resolvedOptions.Handlers.ListPromptsHandler is not null).IsTrue();
    await Assert.That(!ReferenceEquals(
        resolvedOptions.Handlers.ListPromptsHandler, existingHandler)).IsTrue();
  }

  [Test]
  public async Task Configure_WrapsExistingListResourcesHandler()
  {
    var services = new ServiceCollection();
    services.AddOptions();

    var existingHandler = new McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>(
        (_, _) => ValueTask.FromResult(new ListResourcesResult { Resources = [] }));

    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers
      {
        ListResourcesHandler = existingHandler,
      };
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
    });

    var builder = new TestMcpServerBuilder(services);
    builder.AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var resolvedOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(resolvedOptions.Handlers.ListResourcesHandler is not null).IsTrue();
    await Assert.That(!ReferenceEquals(
        resolvedOptions.Handlers.ListResourcesHandler, existingHandler)).IsTrue();
  }

  [Test]
  public async Task Configure_WithExistingAllHandlers_WrapsAllOfThem()
  {
    var services = new ServiceCollection();
    services.AddOptions();

    var existingTools = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [] }));
    var existingPrompts = new McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>(
        (_, _) => ValueTask.FromResult(new ListPromptsResult { Prompts = [] }));
    var existingResources = new McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>(
        (_, _) => ValueTask.FromResult(new ListResourcesResult { Resources = [] }));

    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers
      {
        ListToolsHandler = existingTools,
        ListPromptsHandler = existingPrompts,
        ListResourcesHandler = existingResources,
      };
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
    });

    var builder = new TestMcpServerBuilder(services);
    builder.AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var resolvedOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(resolvedOptions.Handlers.ListToolsHandler is not null).IsTrue();
    await Assert.That(resolvedOptions.Handlers.ListPromptsHandler is not null).IsTrue();
    await Assert.That(resolvedOptions.Handlers.ListResourcesHandler is not null).IsTrue();

    await Assert.That(!ReferenceEquals(
        resolvedOptions.Handlers.ListToolsHandler, existingTools)).IsTrue();
    await Assert.That(!ReferenceEquals(
        resolvedOptions.Handlers.ListPromptsHandler, existingPrompts)).IsTrue();
    await Assert.That(!ReferenceEquals(
        resolvedOptions.Handlers.ListResourcesHandler, existingResources)).IsTrue();
  }

  [Test]
  public async Task Configure_HandlersSet_EvenWhenNoExistingHandlers()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers();
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
    });

    var builder = new TestMcpServerBuilder(services);
    builder.AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var resolvedOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(resolvedOptions.Handlers.ListToolsHandler is not null).IsTrue();
    await Assert.That(resolvedOptions.Handlers.ListPromptsHandler is not null).IsTrue();
    await Assert.That(resolvedOptions.Handlers.ListResourcesHandler is not null).IsTrue();
  }

  [Test]
  public async Task Configure_WithPopulatedCollections_UsesFullListForEachType()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers();
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
    });

    services.AddMcpServer()
        .WithCapabilityAwareTools<TestTools>()
        .WithPrompts<TestPrompts>()
        .WithResources<TestResources>()
        .AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(options.Handlers.ListToolsHandler is not null).IsTrue();
    await Assert.That(options.Handlers.ListPromptsHandler is not null).IsTrue();
    await Assert.That(options.Handlers.ListResourcesHandler is not null).IsTrue();

    await Assert.That(options.ToolCollection is null).IsTrue();
    await Assert.That(options.PromptCollection is null).IsTrue();
    await Assert.That(options.ResourceCollection is null).IsTrue();
  }

  [Test]
  public async Task Configure_EmptyToolCollection_ReturnsEmptyList()
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

    await Assert.That(options.Handlers.ListToolsHandler is not null).IsTrue();
    await Assert.That(options.ToolCollection is null).IsTrue();
  }

  [Test]
  public async Task Configure_NullCollectionsPath_AllHandlersSet()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers();
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
      opt.ToolCollection = null;
      opt.PromptCollection = null;
      opt.ResourceCollection = null;
    });

    var builder = new TestMcpServerBuilder(services);
    builder.AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var resolvedOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    await Assert.That(resolvedOptions.Handlers.ListToolsHandler is not null).IsTrue();
    await Assert.That(resolvedOptions.Handlers.ListPromptsHandler is not null).IsTrue();
    await Assert.That(resolvedOptions.Handlers.ListResourcesHandler is not null).IsTrue();
  }
}