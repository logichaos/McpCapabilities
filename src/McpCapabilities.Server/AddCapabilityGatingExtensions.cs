using McpCapabilities.Server;

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
            sp.GetRequiredService<IOptions<CapabilityGatingOptions>>()));

    return builder;
  }

  private sealed class CapabilityGatingConfigureOptions : IConfigureOptions<McpServerOptions>
  {
    private readonly CapabilityGatingOptions _gatingOptions;

    public CapabilityGatingConfigureOptions(IOptions<CapabilityGatingOptions> options)
    {
      _gatingOptions = options.Value;
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
          if (_gatingOptions.OnToolRequirements is { } onTool)
          {
            var reqs = tool.GetCapabilityRequirements();
            if (reqs.Required != CapabilityFlag.None)
              onTool(tool, reqs);
          }
        }
      }

      if (options.PromptCollection is not null)
      {
        foreach (var prompt in options.PromptCollection)
        {
          prompt.CaptureCapabilityRequirements();
          if (_gatingOptions.OnPromptRequirements is { } onPrompt)
          {
            var reqs = prompt.GetCapabilityRequirements();
            if (reqs.Required != CapabilityFlag.None)
              onPrompt(prompt, reqs);
          }
        }
      }

      if (options.ResourceCollection is not null)
      {
        foreach (var resource in options.ResourceCollection)
        {
          resource.CaptureCapabilityRequirements();
          if (_gatingOptions.OnResourceRequirements is { } onResource)
          {
            var reqs = resource.GetCapabilityRequirements();
            if (reqs.Required != CapabilityFlag.None)
              onResource(resource, reqs);
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
          allow);

      options.Handlers.ListPromptsHandler = CapabilityFilteringHandlers.WrapListPrompts(
          existingListPrompts is not null
              ? CombineHandlers(listPromptsInner, existingListPrompts)
              : listPromptsInner,
          GetClientCaps,
          allow);

      options.Handlers.ListResourcesHandler = CapabilityFilteringHandlers.WrapListResources(
          existingListResources is not null
              ? CombineHandlers(listResourcesInner, existingListResources)
              : listResourcesInner,
          GetClientCaps,
          allow);
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
          var clientCaps = request.Server?.ClientCapabilities;
          var reqs = serverTool.GetCapabilityRequirements();
          if (!CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allow))
          {
            var missing = reqs.Required & ~CapabilityFlags.FromClientCapabilities(clientCaps);
            throw new McpProtocolException(
                reqs.Message ?? $"Client missing capabilities to call '{toolName}': {missing}",
                McpErrorCode.InvalidRequest);
          }
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
          var clientCaps = request.Server?.ClientCapabilities;
          var reqs = serverPrompt.GetCapabilityRequirements();
          if (!CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allow))
          {
            var missing = reqs.Required & ~CapabilityFlags.FromClientCapabilities(clientCaps);
            throw new McpProtocolException(
                reqs.Message ?? $"Client missing capabilities to get '{promptName}': {missing}",
                McpErrorCode.InvalidRequest);
          }
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
          var clientCaps = request.Server?.ClientCapabilities;
          var reqs = serverResource.GetCapabilityRequirements();
          if (!CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allow))
          {
            var missing = reqs.Required & ~CapabilityFlags.FromClientCapabilities(clientCaps);
            throw new McpProtocolException(
                reqs.Message ?? $"Client missing capabilities to read '{uri}': {missing}",
                McpErrorCode.InvalidRequest);
          }
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
