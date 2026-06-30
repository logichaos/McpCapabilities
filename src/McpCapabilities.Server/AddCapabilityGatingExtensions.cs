using McpCapabilities.Server;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

public class CapabilityGatingOptions
{
  /// <summary>
  /// When false (default), clients that send no ClientCapabilities object at all are subject to
  /// capability gating. Set to true to allow those clients to bypass gating and see all primitives.
  ///
  /// Can be configured via appsettings: { "CapabilityGating": { "AllowWhenClientCapabilitiesNotProvided": true } }
  /// </summary>
  public bool AllowWhenClientCapabilitiesNotProvided { get; set; } = false;

  public Action<McpServerTool, ClientCapabilityRequirements>? OnToolRequirements { get; set; }
  public Action<McpServerPrompt, ClientCapabilityRequirements>? OnPromptRequirements { get; set; }
  public Action<McpServerResource, ClientCapabilityRequirements>? OnResourceRequirements { get; set; }
}

public static class AddCapabilityGatingExtensions
{
  public static IMcpServerBuilder AddCapabilityGating(
      this IMcpServerBuilder builder,
      Action<CapabilityGatingOptions>? configureOptions = null)
  {
    builder.Services.AddOptions<CapabilityGatingOptions>();

    if (configureOptions is not null)
      builder.Services.Configure(configureOptions);

    builder.Services.AddSingleton<IConfigureOptions<McpServerOptions>>(sp =>
        new CapabilityGatingConfigureOptions(
            sp.GetRequiredService<IOptions<CapabilityGatingOptions>>(),
            sp.GetService<ILoggerFactory>()));

    return builder;
  }

  private sealed class CapabilityGatingConfigureOptions : IConfigureOptions<McpServerOptions>
  {
    private readonly CapabilityGatingOptions _gatingOptions;
    private readonly ILogger? _listToolsLogger;
    private readonly ILogger? _listPromptsLogger;
    private readonly ILogger? _listResourcesLogger;
    private readonly ILogger? _dispatchLogger;
    private readonly ILogger? _registrationLogger;

    public CapabilityGatingConfigureOptions(
        IOptions<CapabilityGatingOptions> options,
        ILoggerFactory? loggerFactory = null)
    {
      _gatingOptions = options.Value;
      _listToolsLogger = loggerFactory?.CreateLogger("McpCapabilities.ListTools");
      _listPromptsLogger = loggerFactory?.CreateLogger("McpCapabilities.ListPrompts");
      _listResourcesLogger = loggerFactory?.CreateLogger("McpCapabilities.ListResources");
      _dispatchLogger = loggerFactory?.CreateLogger("McpCapabilities.Dispatch");
      _registrationLogger = loggerFactory?.CreateLogger("McpCapabilities.Registration");
    }

    public void Configure(McpServerOptions options)
    {
      CaptureAndNotifyRequirements(options);
      BuildAndWrapHandlers(options);
      BuildDispatchHandlers(options);
      ClearCollections(options);
    }

    private void CaptureAndNotifyRequirements(McpServerOptions options)
    {
      if (options.ToolCollection is not null)
      {
        foreach (var tool in options.ToolCollection)
        {
          tool.CaptureCapabilityRequirements();
          var reqs = tool.GetCapabilityRequirements();
          if (reqs.Required != CapabilityFlag.None)
          {
            _registrationLogger?.LogInformation("Tool '{ToolName}' requires {Flags}", tool.ProtocolTool.Name, reqs.Required);
            _gatingOptions.OnToolRequirements?.Invoke(tool, reqs);
          }
        }
      }

      if (options.PromptCollection is not null)
      {
        foreach (var prompt in options.PromptCollection)
        {
          prompt.CaptureCapabilityRequirements();
          var reqs = prompt.GetCapabilityRequirements();
          if (reqs.Required != CapabilityFlag.None)
          {
            _registrationLogger?.LogInformation("Prompt '{PromptName}' requires {Flags}", prompt.ProtocolPrompt.Name, reqs.Required);
            _gatingOptions.OnPromptRequirements?.Invoke(prompt, reqs);
          }
        }
      }

      if (options.ResourceCollection is not null)
      {
        foreach (var resource in options.ResourceCollection)
        {
          resource.CaptureCapabilityRequirements();
          var reqs = resource.GetCapabilityRequirements();
          if (reqs.Required != CapabilityFlag.None)
          {
            _registrationLogger?.LogInformation("Resource '{ResourceName}' requires {Flags}", resource.ProtocolResource?.Name ?? resource.ProtocolResource?.Uri ?? "", reqs.Required);
            _gatingOptions.OnResourceRequirements?.Invoke(resource, reqs);
          }
        }
      }
    }

    private void BuildAndWrapHandlers(McpServerOptions options)
    {
      static ClientCapabilities? GetClientCaps<TParams>(RequestContext<TParams> request)
          => request.Server?.ClientCapabilities;

      var serverTools = options.ToolCollection?.ToList() ?? [];
      var serverPrompts = options.PromptCollection?.ToList() ?? [];
      var serverResources = options.ResourceCollection?.ToList() ?? [];

      var toolList = serverTools.Select(t => t.ProtocolTool).ToList();
      var promptList = serverPrompts.Select(p => p.ProtocolPrompt).ToList();
      var resourceList = serverResources
          .Select(r => r.ProtocolResource)
          .OfType<Resource>()
          .ToList();

      var listToolsInner = BuildInnerListToolsHandler(toolList);
      var listPromptsInner = BuildInnerListPromptsHandler(promptList);
      var listResourcesInner = BuildInnerListResourcesHandler(resourceList);

      var existingListTools = options.Handlers.ListToolsHandler;
      var existingListPrompts = options.Handlers.ListPromptsHandler;
      var existingListResources = options.Handlers.ListResourcesHandler;

      var allow = _gatingOptions.AllowWhenClientCapabilitiesNotProvided;

      options.Handlers.ListToolsHandler = CapabilityFilteringHandlers.WrapListTools(
          existingListTools is not null
              ? CombineHandlers(listToolsInner, existingListTools)
              : listToolsInner,
          GetClientCaps,
          allow,
          logger: _listToolsLogger);

      options.Handlers.ListPromptsHandler = CapabilityFilteringHandlers.WrapListPrompts(
          existingListPrompts is not null
              ? CombineHandlers(listPromptsInner, existingListPrompts)
              : listPromptsInner,
          GetClientCaps,
          allow,
          logger: _listPromptsLogger);

      options.Handlers.ListResourcesHandler = CapabilityFilteringHandlers.WrapListResources(
          existingListResources is not null
              ? CombineHandlers(listResourcesInner, existingListResources)
              : listResourcesInner,
          GetClientCaps,
          allow,
          logger: _listResourcesLogger);
    }

    private void BuildDispatchHandlers(McpServerOptions options)
    {
      var serverTools = options.ToolCollection?.ToList() ?? [];
      var serverPrompts = options.PromptCollection?.ToList() ?? [];
      var serverResources = options.ResourceCollection?.ToList() ?? [];

      var allow = _gatingOptions.AllowWhenClientCapabilitiesNotProvided;

      var toolLookup = serverTools.ToDictionary(t => t.ProtocolTool.Name);
      var existingCallTool = options.Handlers.CallToolHandler;

      options.Handlers.CallToolHandler = async (request, ct) =>
      {
        if (request.Params?.Name is { } toolName &&
            toolLookup.TryGetValue(toolName, out var serverTool))
        {
          using var activity = McpCapabilitiesTelemetry.Source.StartActivity("dispatch tools/call");
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.PrimitiveType, "tool");
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.PrimitiveName, toolName);

          var clientCaps = request.Server?.ClientCapabilities;
          var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
          var reqs = serverTool.GetCapabilityRequirements();
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.ClientFlags, clientFlags.ToString());
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.RequiredFlags, reqs.Required.ToString());

          if (!CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allow))
          {
            var missing = reqs.Required & ~clientFlags;
            activity?.SetTag(McpCapabilitiesTelemetry.Tags.MissingFlags, missing.ToString());
            activity?.SetTag(McpCapabilitiesTelemetry.Tags.Allowed, false);
            _dispatchLogger?.LogWarning("Tool '{ToolName}' denied (requires {Required}, client missing {Missing})", toolName, reqs.Required, missing);
            throw new McpProtocolException(
                reqs.Message ?? $"Client missing capabilities to call '{toolName}': {missing}",
                McpErrorCode.InvalidRequest);
          }
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.Allowed, true);
          if (reqs.Required != CapabilityFlag.None)
            _dispatchLogger?.LogDebug("Tool '{ToolName}' allowed (requires {Required}, client has {ClientFlags})", toolName, reqs.Required, clientFlags);
          return await serverTool.InvokeAsync(request, ct);
        }

        if (existingCallTool is not null)
          return await existingCallTool(request, ct);

        throw new McpProtocolException(
            $"Unknown tool: '{request.Params?.Name}'",
            McpErrorCode.InvalidParams);
      };

      var promptLookup = serverPrompts.ToDictionary(p => p.ProtocolPrompt.Name);
      var existingGetPrompt = options.Handlers.GetPromptHandler;

      options.Handlers.GetPromptHandler = async (request, ct) =>
      {
        if (request.Params?.Name is { } promptName &&
            promptLookup.TryGetValue(promptName, out var serverPrompt))
        {
          using var activity = McpCapabilitiesTelemetry.Source.StartActivity("dispatch prompts/get");
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.PrimitiveType, "prompt");
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.PrimitiveName, promptName);

          var clientCaps = request.Server?.ClientCapabilities;
          var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
          var reqs = serverPrompt.GetCapabilityRequirements();
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.ClientFlags, clientFlags.ToString());
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.RequiredFlags, reqs.Required.ToString());

          if (!CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allow))
          {
            var missing = reqs.Required & ~clientFlags;
            activity?.SetTag(McpCapabilitiesTelemetry.Tags.MissingFlags, missing.ToString());
            activity?.SetTag(McpCapabilitiesTelemetry.Tags.Allowed, false);
            _dispatchLogger?.LogWarning("Prompt '{PromptName}' denied (requires {Required}, client missing {Missing})", promptName, reqs.Required, missing);
            throw new McpProtocolException(
                reqs.Message ?? $"Client missing capabilities to get '{promptName}': {missing}",
                McpErrorCode.InvalidRequest);
          }
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.Allowed, true);
          if (reqs.Required != CapabilityFlag.None)
            _dispatchLogger?.LogDebug("Prompt '{PromptName}' allowed (requires {Required}, client has {ClientFlags})", promptName, reqs.Required, clientFlags);
          return await serverPrompt.GetAsync(request, ct);
        }

        if (existingGetPrompt is not null)
          return await existingGetPrompt(request, ct);

        throw new McpProtocolException(
            $"Unknown prompt: '{request.Params?.Name}'",
            McpErrorCode.InvalidParams);
      };

      var resourceLookup = serverResources.ToDictionary(r => r.ProtocolResource?.Uri ?? "");
      var existingReadResource = options.Handlers.ReadResourceHandler;

      options.Handlers.ReadResourceHandler = async (request, ct) =>
      {
        if (request.Params?.Uri is { } uri &&
            resourceLookup.TryGetValue(uri, out var serverResource))
        {
          using var activity = McpCapabilitiesTelemetry.Source.StartActivity("dispatch resources/read");
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.PrimitiveType, "resource");
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.PrimitiveName, uri);

          var clientCaps = request.Server?.ClientCapabilities;
          var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
          var reqs = serverResource.GetCapabilityRequirements();
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.ClientFlags, clientFlags.ToString());
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.RequiredFlags, reqs.Required.ToString());

          if (!CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allow))
          {
            var missing = reqs.Required & ~clientFlags;
            activity?.SetTag(McpCapabilitiesTelemetry.Tags.MissingFlags, missing.ToString());
            activity?.SetTag(McpCapabilitiesTelemetry.Tags.Allowed, false);
            _dispatchLogger?.LogWarning("Resource '{ResourceUri}' denied (requires {Required}, client missing {Missing})", uri, reqs.Required, missing);
            throw new McpProtocolException(
                reqs.Message ?? $"Client missing capabilities to read '{uri}': {missing}",
                McpErrorCode.InvalidRequest);
          }
          activity?.SetTag(McpCapabilitiesTelemetry.Tags.Allowed, true);
          if (reqs.Required != CapabilityFlag.None)
            _dispatchLogger?.LogDebug("Resource '{ResourceUri}' allowed (requires {Required}, client has {ClientFlags})", uri, reqs.Required, clientFlags);
          return await serverResource.ReadAsync(request, ct);
        }

        if (existingReadResource is not null)
          return await existingReadResource(request, ct);

        throw new McpProtocolException(
            $"Unknown resource URI: '{request.Params?.Uri}'",
            McpErrorCode.InvalidParams);
      };
    }

    private static void ClearCollections(McpServerOptions options)
    {
      options.ToolCollection = null;
      options.PromptCollection = null;
      options.ResourceCollection = null;
    }

    private static McpRequestHandler<ListToolsRequestParams, ListToolsResult>
        BuildInnerListToolsHandler(List<Tool> tools) =>
        (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = tools });

    private static McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>
        BuildInnerListPromptsHandler(List<Prompt> prompts) =>
        (_, _) => ValueTask.FromResult(new ListPromptsResult { Prompts = prompts });

    private static McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>
        BuildInnerListResourcesHandler(List<Resource> resources) =>
        (_, _) => ValueTask.FromResult(new ListResourcesResult { Resources = resources });

    private static McpRequestHandler<TParams, ListToolsResult> CombineHandlers<TParams>(
        McpRequestHandler<TParams, ListToolsResult> first,
        McpRequestHandler<TParams, ListToolsResult> second)
    {
      return async (request, ct) =>
      {
        var result1 = await first(request, ct);
        var result2 = await second(request, ct);
        var combined = new List<Tool>(result1.Tools);
        combined.AddRange(result2.Tools);
        return new ListToolsResult { Tools = combined };
      };
    }

    private static McpRequestHandler<TParams, ListPromptsResult> CombineHandlers<TParams>(
        McpRequestHandler<TParams, ListPromptsResult> first,
        McpRequestHandler<TParams, ListPromptsResult> second)
    {
      return async (request, ct) =>
      {
        var result1 = await first(request, ct);
        var result2 = await second(request, ct);
        var combined = new List<Prompt>(result1.Prompts);
        combined.AddRange(result2.Prompts);
        return new ListPromptsResult { Prompts = combined };
      };
    }

    private static McpRequestHandler<TParams, ListResourcesResult> CombineHandlers<TParams>(
        McpRequestHandler<TParams, ListResourcesResult> first,
        McpRequestHandler<TParams, ListResourcesResult> second)
    {
      return async (request, ct) =>
      {
        var result1 = await first(request, ct);
        var result2 = await second(request, ct);
        var combined = new List<Resource>(result1.Resources);
        combined.AddRange(result2.Resources);
        return new ListResourcesResult { Resources = combined };
      };
    }
  }
}
