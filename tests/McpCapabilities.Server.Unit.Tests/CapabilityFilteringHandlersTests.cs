using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using FakeItEasy;
using Microsoft.Extensions.Logging;

using McpCapabilities.Server;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class CapabilityFilteringHandlersTests
{
  private static Tool CreateTool(string name, CapabilityFlag? required = null)
  {
    var tool = new Tool { Name = name };
    if (required.HasValue && required.Value != CapabilityFlag.None)
    {
      var reqs = new ClientCapabilityRequirements { Required = required.Value };
      tool.Meta ??= [];
      reqs.WriteToMeta(tool.Meta);
    }
    return tool;
  }

  [Test]
  public async Task WrapListTools_FiltersUnsatisfiedTools()
  {
    var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (request, ct) =>
        {
          var result = new ListToolsResult
          {
            Tools =
                  [
                      CreateTool("sampling_tool", CapabilityFlag.Sampling),
                        CreateTool("no_reqs_tool"),
                ],
          };
          return ValueTask.FromResult(result);
        });

    var wrapped = CapabilityFilteringHandlers.WrapListTools(
        innerHandler, _ => new ClientCapabilities { Roots = new RootsCapability() });
    var result = await wrapped(default!, default);

    await Assert.That(result.Tools).Count().IsEqualTo(1);
    await Assert.That(result.Tools[0].Name).IsEqualTo("no_reqs_tool");
  }

  [Test]
  public async Task WrapListTools_NoCapabilities_ShowsAllTools()
  {
    var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (request, ct) =>
        {
          var result = new ListToolsResult
          {
            Tools =
                  [
                      CreateTool("sampling_tool", CapabilityFlag.Sampling),
                        CreateTool("no_reqs_tool"),
                ],
          };
          return ValueTask.FromResult(result);
        });

    var wrapped = CapabilityFilteringHandlers.WrapListTools(innerHandler, _ => null);
    var result = await wrapped(default!, default);

    await Assert.That(result.Tools).Count().IsEqualTo(2);
  }

  [Test]
  public async Task WrapListTools_PreservesSatisfiedTools()
  {
    var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (request, ct) =>
        {
          var result = new ListToolsResult
          {
            Tools =
                  [
                      CreateTool("sampling_tool", CapabilityFlag.Sampling),
                        CreateTool("roots_tool", CapabilityFlag.Roots),
                ],
          };
          return ValueTask.FromResult(result);
        });

    var wrapped = CapabilityFilteringHandlers.WrapListTools(
        innerHandler, _ => new ClientCapabilities { Sampling = new SamplingCapability() });
    var result = await wrapped(default!, default);

    await Assert.That(result.Tools).Count().IsEqualTo(1);
    await Assert.That(result.Tools[0].Name).IsEqualTo("sampling_tool");
  }

  [Test]
  public async Task WrapListTools_UnannotatedToolsAlwaysVisible()
  {
    var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (request, ct) =>
        {
          var result = new ListToolsResult
          {
            Tools =
                  [
                      CreateTool("tool1"),
                        CreateTool("tool2"),
                ],
          };
          return ValueTask.FromResult(result);
        });

    var wrapped = CapabilityFilteringHandlers.WrapListTools(innerHandler, _ => new ClientCapabilities());
    var result = await wrapped(default!, default);

    await Assert.That(result.Tools).Count().IsEqualTo(2);
  }

  [Test]
  public async Task WrapListTools_NullInnerHandler_ReturnsEmptyList()
  {
    var wrapped = CapabilityFilteringHandlers.WrapListTools(
        null, _ => new ClientCapabilities { Sampling = new SamplingCapability() });
    var result = await wrapped(default!, default);

    await Assert.That(result.Tools).IsNotNull();
    await Assert.That(result.Tools).Count().IsEqualTo(0);
  }

  [Test]
  public async Task WrapListPrompts_FiltersUnsatisfiedPrompts()
  {
    var samplingPrompt = new Prompt { Name = "sampling_prompt" };
    var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Sampling };
    samplingPrompt.Meta ??= [];
    reqs.WriteToMeta(samplingPrompt.Meta);

    var nonePrompt = new Prompt { Name = "no_reqs_prompt" };

    var innerHandler = new McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>(
        (request, ct) =>
        {
          var result = new ListPromptsResult
          {
            Prompts = [samplingPrompt, nonePrompt],
          };
          return ValueTask.FromResult(result);
        });

    var wrapped = CapabilityFilteringHandlers.WrapListPrompts(
        innerHandler, _ => new ClientCapabilities { Roots = new RootsCapability() });
    var result = await wrapped(default!, default);

    await Assert.That(result.Prompts).Count().IsEqualTo(1);
    await Assert.That(result.Prompts[0].Name).IsEqualTo("no_reqs_prompt");
  }

  [Test]
  public async Task WrapListPrompts_NoCapabilities_ShowsAllPrompts()
  {
    var samplingPrompt = new Prompt { Name = "sampling_prompt" };
    var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Sampling };
    samplingPrompt.Meta ??= [];
    reqs.WriteToMeta(samplingPrompt.Meta);

    var nonePrompt = new Prompt { Name = "no_reqs_prompt" };

    var innerHandler = new McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>(
        (request, ct) => ValueTask.FromResult(new ListPromptsResult
        {
          Prompts = [samplingPrompt, nonePrompt],
        }));

    var wrapped = CapabilityFilteringHandlers.WrapListPrompts(innerHandler, _ => null);
    var result = await wrapped(default!, default);

    await Assert.That(result.Prompts).Count().IsEqualTo(2);
  }

  [Test]
  public async Task WrapListResources_FiltersUnsatisfiedResources()
  {
    var rootsResource = new Resource { Name = "roots_resource", Uri = "resource://roots" };
    var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Roots };
    rootsResource.Meta ??= [];
    reqs.WriteToMeta(rootsResource.Meta);

    var noneResource = new Resource { Name = "no_reqs_resource", Uri = "resource://none" };

    var innerHandler = new McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>(
        (request, ct) =>
        {
          var result = new ListResourcesResult
          {
            Resources = [rootsResource, noneResource],
          };
          return ValueTask.FromResult(result);
        });

    var wrapped = CapabilityFilteringHandlers.WrapListResources(
        innerHandler, _ => new ClientCapabilities { Sampling = new SamplingCapability() });
    var result = await wrapped(default!, default);

    await Assert.That(result.Resources).Count().IsEqualTo(1);
    await Assert.That(result.Resources[0].Name).IsEqualTo("no_reqs_resource");
  }

  [Test]
  public async Task WrapListResources_NoCapabilities_ShowsAllResources()
  {
    var rootsResource = new Resource { Name = "roots_resource", Uri = "resource://roots" };
    var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Roots };
    rootsResource.Meta ??= [];
    reqs.WriteToMeta(rootsResource.Meta);

    var noneResource = new Resource { Name = "no_reqs_resource", Uri = "resource://none" };

    var innerHandler = new McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>(
        (request, ct) => ValueTask.FromResult(new ListResourcesResult
        {
          Resources = [rootsResource, noneResource],
        }));

    var wrapped = CapabilityFilteringHandlers.WrapListResources(innerHandler, _ => null);
    var result = await wrapped(default!, default);

    await Assert.That(result.Resources).Count().IsEqualTo(2);
  }

  // --- Logging behavior ---

  private static (ILogger Logger, List<(LogLevel Level, string Message)> Captured) CreateCapturingLogger()
  {
    var captured = new List<(LogLevel, string)>();
    var logger = A.Fake<ILogger>();
    A.CallTo(logger).Where(call => call.Method.Name == "Log")
        .Invokes(call =>
        {
          var level = (LogLevel)call.Arguments[0]!;
          var state = call.Arguments[2]!;
          var exception = (Exception?)call.Arguments[3];
          var formatter = call.Arguments[4]!;
          var invoke = formatter.GetType().GetMethod("Invoke")!;
          var msg = (string)invoke.Invoke(formatter, [state, exception])!;
          captured.Add((level, msg));
        });
    return (logger, captured);
  }

  [Test]
  public async Task WrapListTools_WithLogger_LogsCorrectSummaryMessage()
  {
    var tools = new[]
    {
      CreateTool("gated_tool", CapabilityFlag.Sampling),
      CreateTool("free_tool"),
    };
    var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [.. tools] }));

    var (logger, captured) = CreateCapturingLogger();
    var wrapped = CapabilityFilteringHandlers.WrapListTools(
        innerHandler, _ => new ClientCapabilities { Sampling = new SamplingCapability() },
        logger: logger);

    await wrapped(default!, default);

    var info = captured.Where(c => c.Level == LogLevel.Information).ToList();
    await Assert.That(info).Count().IsEqualTo(1);
    await Assert.That(info[0].Message).Contains("Tools list:");
    await Assert.That(info[0].Message).Contains("2 of 2");

    var debug = captured.Where(c => c.Level == LogLevel.Debug).ToList();
    await Assert.That(debug).Count().IsEqualTo(2);
    await Assert.That(debug.Any(d => d.Message.Contains("Filtering tools list"))).IsTrue();
    await Assert.That(debug.Any(d => d.Message.Contains("gated_tool") && d.Message.Contains("included"))).IsTrue();
  }

  [Test]
  public async Task WrapListTools_WithLogger_LogsExclusionWithToolName()
  {
    var tools = new[]
    {
      CreateTool("needs_sampling", CapabilityFlag.Sampling),
      CreateTool("needs_roots", CapabilityFlag.Roots),
    };
    var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [.. tools] }));

    var (logger, captured) = CreateCapturingLogger();
    var wrapped = CapabilityFilteringHandlers.WrapListTools(
        innerHandler, _ => new ClientCapabilities { Sampling = new SamplingCapability() },
        logger: logger);

    await wrapped(default!, default);

    var debug = captured.Where(c => c.Level == LogLevel.Debug).ToList();
    await Assert.That(debug).Count().IsEqualTo(3);
    await Assert.That(debug.Any(d => d.Message.Contains("needs_sampling") && d.Message.Contains("included"))).IsTrue();
    await Assert.That(debug.Any(d => d.Message.Contains("needs_roots") && d.Message.Contains("excluded"))).IsTrue();

    var info = captured.Where(c => c.Level == LogLevel.Information).ToList();
    await Assert.That(info).Count().IsEqualTo(1);
    await Assert.That(info[0].Message).Contains("1 of 2");
  }

  [Test]
  public async Task WrapListTools_NullLogger_DoesNotThrow()
  {
    var tools = new[] { CreateTool("any_tool", CapabilityFlag.Sampling) };
    var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [.. tools] }));

    var wrapped = CapabilityFilteringHandlers.WrapListTools(
        innerHandler, _ => new ClientCapabilities(),
        logger: null);

    var result = await wrapped(default!, default);
    await Assert.That(result.Tools).Count().IsEqualTo(0);
  }

  [Test]
  public async Task WrapListPrompts_WithLogger_LogsCorrectSummaryMessage()
  {
    var prompt = new Prompt { Name = "gated_prompt" };
    var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Elicitation };
    prompt.Meta ??= [];
    reqs.WriteToMeta(prompt.Meta);

    var innerHandler = new McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>(
        (_, _) => ValueTask.FromResult(new ListPromptsResult { Prompts = [prompt] }));

    var (logger, captured) = CreateCapturingLogger();
    var wrapped = CapabilityFilteringHandlers.WrapListPrompts(
        innerHandler, _ => new ClientCapabilities { Elicitation = new ElicitationCapability() },
        logger: logger);

    await wrapped(default!, default);

    var info = captured.Where(c => c.Level == LogLevel.Information).ToList();
    await Assert.That(info).Count().IsEqualTo(1);
    await Assert.That(info[0].Message).Contains("Prompts list:");
    await Assert.That(info[0].Message).Contains("1 of 1");

    var debug = captured.Where(c => c.Level == LogLevel.Debug).ToList();
    await Assert.That(debug.Any(d => d.Message.Contains("Filtering prompts list"))).IsTrue();
    await Assert.That(debug.Any(d => d.Message.Contains("gated_prompt") && d.Message.Contains("included"))).IsTrue();
  }

  [Test]
  public async Task WrapListResources_WithLogger_LogsCorrectSummaryMessage()
  {
    var resource = new Resource { Name = "gated_resource", Uri = "res://gated" };
    var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Roots };
    resource.Meta ??= [];
    reqs.WriteToMeta(resource.Meta);

    var innerHandler = new McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>(
        (_, _) => ValueTask.FromResult(new ListResourcesResult { Resources = [resource] }));

    var (logger, captured) = CreateCapturingLogger();
    var wrapped = CapabilityFilteringHandlers.WrapListResources(
        innerHandler, _ => new ClientCapabilities { Roots = new RootsCapability() },
        logger: logger);

    await wrapped(default!, default);

    var info = captured.Where(c => c.Level == LogLevel.Information).ToList();
    await Assert.That(info).Count().IsEqualTo(1);
    await Assert.That(info[0].Message).Contains("Resources list:");
    await Assert.That(info[0].Message).Contains("1 of 1");

    var debug = captured.Where(c => c.Level == LogLevel.Debug).ToList();
    await Assert.That(debug.Any(d => d.Message.Contains("Filtering resources list"))).IsTrue();
    await Assert.That(debug.Any(d => d.Message.Contains("gated_resource") && d.Message.Contains("included"))).IsTrue();
  }

  // --- ActivitySource / OTEL spans ---


  [Test]
  public async Task WrapListPrompts_WithLogger_LogsExclusionWithPromptName()
  {
    var samplingPrompt = new Prompt { Name = "needs_sampling" };
    var rootsPrompt = new Prompt { Name = "needs_roots" };
    var samplingReqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Sampling };
    var rootsReqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Roots };
    samplingPrompt.Meta ??= [];
    rootsPrompt.Meta ??= [];
    samplingReqs.WriteToMeta(samplingPrompt.Meta);
    rootsReqs.WriteToMeta(rootsPrompt.Meta);

    var innerHandler = new McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>(
        (_, _) => ValueTask.FromResult(new ListPromptsResult { Prompts = [samplingPrompt, rootsPrompt] }));

    var (logger, captured) = CreateCapturingLogger();
    var wrapped = CapabilityFilteringHandlers.WrapListPrompts(
        innerHandler, _ => new ClientCapabilities { Sampling = new SamplingCapability() },
        logger: logger);

    await wrapped(default!, default);

    var debug = captured.Where(c => c.Level == LogLevel.Debug).ToList();
    await Assert.That(debug.Any(d => d.Message.Contains("needs_sampling") && d.Message.Contains("included"))).IsTrue();
    await Assert.That(debug.Any(d => d.Message.Contains("needs_roots") && d.Message.Contains("excluded"))).IsTrue();
  }

  [Test]
  public async Task WrapListResources_WithLogger_LogsExclusionWithResourceName()
  {
    var samplingRes = new Resource { Name = "needs_sampling", Uri = "res://s" };
    var rootsRes = new Resource { Name = "needs_roots", Uri = "res://r" };
    var samplingReqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Sampling };
    var rootsReqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Roots };
    samplingRes.Meta ??= [];
    rootsRes.Meta ??= [];
    samplingReqs.WriteToMeta(samplingRes.Meta);
    rootsReqs.WriteToMeta(rootsRes.Meta);

    var innerHandler = new McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>(
        (_, _) => ValueTask.FromResult(new ListResourcesResult { Resources = [samplingRes, rootsRes] }));

    var (logger, captured) = CreateCapturingLogger();
    var wrapped = CapabilityFilteringHandlers.WrapListResources(
        innerHandler, _ => new ClientCapabilities { Sampling = new SamplingCapability() },
        logger: logger);

    await wrapped(default!, default);

    var debug = captured.Where(c => c.Level == LogLevel.Debug).ToList();
    await Assert.That(debug.Any(d => d.Message.Contains("needs_sampling") && d.Message.Contains("included"))).IsTrue();
    await Assert.That(debug.Any(d => d.Message.Contains("needs_roots") && d.Message.Contains("excluded"))).IsTrue();
  }

  [Test]
  public async Task WrapListTools_AllowWhenNotProvidedFalse_BlocksWhenNoCapabilities()
  {
    var tools = new[] { CreateTool("gated_tool", CapabilityFlag.Sampling) };
    var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [.. tools] }));

    var wrapped = CapabilityFilteringHandlers.WrapListTools(
        innerHandler, _ => null,
        allowWhenClientCapabilitiesNotProvided: false);
    var result = await wrapped(default!, default);

    await Assert.That(result.Tools).Count().IsEqualTo(0);
  }
  [Test]
  public async Task McpCapabilitiesTelemetry_HasValidActivitySource()
  {
    await Assert.That(McpCapabilitiesTelemetry.Source).IsNotNull();
    await Assert.That(McpCapabilitiesTelemetry.Source.Name).IsEqualTo("McpCapabilities.Server");

    using var activity = McpCapabilitiesTelemetry.Source.StartActivity("test");
    await Assert.That(activity).IsNotNull();
    await Assert.That(activity!.OperationName).IsEqualTo("test");
  }

  [Test]
  public async Task WrapListTools_NullActivitySource_DoesNotThrow()
  {
    var tools = new[] { CreateTool("any_tool", CapabilityFlag.Sampling) };
    var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
        (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [.. tools] }));

    // No listener attached — activity will be null
    var wrapped = CapabilityFilteringHandlers.WrapListTools(
        innerHandler, _ => new ClientCapabilities());
    var result = await wrapped(default!, default);
    await Assert.That(result.Tools).Count().IsEqualTo(0);
  }

  [Test]
  public async Task McpCapabilitiesTelemetry_Tags_HaveExpectedValues()
  {
    await Assert.That(McpCapabilitiesTelemetry.Tags.PrimitiveType).IsEqualTo("mcp.capabilities.primitive_type");
    await Assert.That(McpCapabilitiesTelemetry.Tags.PrimitiveName).IsEqualTo("mcp.capabilities.primitive_name");
    await Assert.That(McpCapabilitiesTelemetry.Tags.ClientFlags).IsEqualTo("mcp.capabilities.client_flags");
    await Assert.That(McpCapabilitiesTelemetry.Tags.RequiredFlags).IsEqualTo("mcp.capabilities.required_flags");
    await Assert.That(McpCapabilitiesTelemetry.Tags.MissingFlags).IsEqualTo("mcp.capabilities.missing_flags");
    await Assert.That(McpCapabilitiesTelemetry.Tags.Allowed).IsEqualTo("mcp.capabilities.allowed");
    await Assert.That(McpCapabilitiesTelemetry.Tags.VisibleCount).IsEqualTo("mcp.capabilities.visible_count");
    await Assert.That(McpCapabilitiesTelemetry.Tags.TotalCount).IsEqualTo("mcp.capabilities.total_count");
  }
}
