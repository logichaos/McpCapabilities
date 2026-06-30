using System.Reflection;
using Microsoft.Extensions.Logging;
using McpCapabilities.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class AddCapabilityGatingExtensionsTests
{
  [Test]
  public async Task AddCapabilityGating_ReturnsBuilderForChaining()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt => opt.Handlers = new McpServerHandlers());
    var builderMock = new TestMcpServerBuilder(services);

    var result = builderMock.AddCapabilityGating();

    await Assert.That(result).IsNotNull();
  }

  [Test]
  public async Task AddCapabilityGating_ConfiguresHandlers()
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt => opt.Handlers = new McpServerHandlers());
    var builderMock = new TestMcpServerBuilder(services);

    builderMock.AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var resolvedOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var listTools = resolvedOptions.Handlers.ListToolsHandler;
    var listPrompts = resolvedOptions.Handlers.ListPromptsHandler;
    var listResources = resolvedOptions.Handlers.ListResourcesHandler;
    await Assert.That(listTools is not null).IsTrue();
    await Assert.That(listPrompts is not null).IsTrue();
    await Assert.That(listResources is not null).IsTrue();
  }

  [Test]
  public async Task AddCapabilityGating_WrapsExistingHandler()
  {
    var services = new ServiceCollection();
    services.AddOptions();

    var existingHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [] }));

    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers
      {
        ListToolsHandler = existingHandler,
      };
    });
    var builderMock = new TestMcpServerBuilder(services);

    builderMock.AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var resolvedOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

    var wrappedHandler = resolvedOptions.Handlers.ListToolsHandler;
    await Assert.That(wrappedHandler is not null).IsTrue();
    await Assert.That(!ReferenceEquals(wrappedHandler, existingHandler)).IsTrue();
  }

  // --- Startup registration logging ---

  [McpServerToolType]
  private sealed class LoggedTestTools
  {
    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Sampling)]
    public string GatedTool() => "result";

    [McpServerTool]
    public string FreeTool() => "free";
  }

  [Test]
  public async Task AddCapabilityGating_WithTools_LogsRequiredCapabilities()
  {
    var captured = new List<(LogLevel Level, string Message)>();
    using var loggerFactory = new LoggerFactory([new CapturingLoggerProvider(captured)]);

    var services = new ServiceCollection();
    services.AddOptions();
    services.AddLogging();
    services.AddSingleton<ILoggerFactory>(loggerFactory);
    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = new McpServerHandlers();
    });

    services.AddMcpServer()
        .WithTools<LoggedTestTools>()
        .AddCapabilityGating();

    _ = services.BuildServiceProvider().GetRequiredService<IOptions<McpServerOptions>>().Value;

    var registrationLogs = captured.Where(c => c.Message.Contains("requires")).ToList();
    await Assert.That(registrationLogs).Count().IsEqualTo(1);
    await Assert.That(registrationLogs[0].Level).IsEqualTo(LogLevel.Information);
    await Assert.That(registrationLogs[0].Message).Contains("gated_tool");
    await Assert.That(registrationLogs[0].Message).Contains("Sampling");
  }


  [McpServerPromptType]
  private sealed class LoggedTestPrompts
  {
    [McpServerPrompt]
    [RequiredClientCapabilities(Required = CapabilityFlag.Elicitation)]
    public string GatedPrompt() => "result";

    [McpServerPrompt]
    public string FreePrompt() => "free";
  }

  [McpServerResourceType]
  private sealed class LoggedTestResources
  {
    [McpServerResource]
    [RequiredClientCapabilities(Required = CapabilityFlag.Roots)]
    public string GatedResource() => "result";

    [McpServerResource]
    public string FreeResource() => "free";
  }

  [Test]
  public async Task AddCapabilityGating_WithPrompts_LogsRequiredCapabilities()
  {
    var captured = new List<(LogLevel Level, string Message)>();
    using var loggerFactory = new LoggerFactory([new CapturingLoggerProvider(captured)]);

    var services = new ServiceCollection();
    services.AddOptions();
    services.AddLogging();
    services.AddSingleton<ILoggerFactory>(loggerFactory);
    services.Configure<McpServerOptions>(opt => opt.Handlers = new McpServerHandlers());

    services.AddMcpServer()
        .WithPrompts<LoggedTestPrompts>()
        .AddCapabilityGating();

    _ = services.BuildServiceProvider().GetRequiredService<IOptions<McpServerOptions>>().Value;

    var registrationLogs = captured.Where(c => c.Message.Contains("requires")).ToList();
    await Assert.That(registrationLogs).Count().IsEqualTo(1);
    await Assert.That(registrationLogs[0].Level).IsEqualTo(LogLevel.Information);
    await Assert.That(registrationLogs[0].Message).Contains("gated_prompt");
    await Assert.That(registrationLogs[0].Message).Contains("Elicitation");
  }

  [Test]
  public async Task AddCapabilityGating_WithResources_LogsRequiredCapabilities()
  {
    var captured = new List<(LogLevel Level, string Message)>();
    using var loggerFactory = new LoggerFactory([new CapturingLoggerProvider(captured)]);

    var services = new ServiceCollection();
    services.AddOptions();
    services.AddLogging();
    services.AddSingleton<ILoggerFactory>(loggerFactory);
    services.Configure<McpServerOptions>(opt => opt.Handlers = new McpServerHandlers());

    services.AddMcpServer()
        .WithResources<LoggedTestResources>()
        .AddCapabilityGating();

    _ = services.BuildServiceProvider().GetRequiredService<IOptions<McpServerOptions>>().Value;

    var registrationLogs = captured.Where(c => c.Message.Contains("requires")).ToList();
    await Assert.That(registrationLogs).Count().IsEqualTo(1);
    await Assert.That(registrationLogs[0].Level).IsEqualTo(LogLevel.Information);
    await Assert.That(registrationLogs[0].Message).Contains("gated_resource");
    await Assert.That(registrationLogs[0].Message).Contains("Roots");
  }
  private sealed class CapturingLoggerProvider(List<(LogLevel Level, string Message)> captured) : ILoggerProvider
  {
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(captured);
    public void Dispose() { }
  }

  private sealed class CapturingLogger(List<(LogLevel Level, string Message)> captured) : ILogger
  {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
      captured.Add((logLevel, formatter(state, exception)));
    }
  }
}

internal sealed class TestMcpServerBuilder : IMcpServerBuilder
{
  public IServiceCollection Services { get; }

  public TestMcpServerBuilder(IServiceCollection services)
  {
    Services = services;
  }
}