using McpCapabilities.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

public static class AddCapabilityGatingExtensions
{
  public static IMcpServerBuilder AddCapabilityGating(this IMcpServerBuilder builder)
  {
    builder.Services.AddSingleton<IConfigureOptions<McpServerOptions>>(
        new CapabilityGatingConfigureOptions());

    return builder;
  }

  private sealed class CapabilityGatingConfigureOptions : IConfigureOptions<McpServerOptions>
  {
    public void Configure(McpServerOptions options)
    {
      // Capture capability requirements from all registered primitives.
      // This runs after all WithTools/WithPrompts/WithResources configure options
      // have populated the collections, ensuring Meta is written before we snapshot.
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

      static CapabilityFlag GetClientFlags<TParams>(RequestContext<TParams> request)
      {
        return CapabilityFlags.FromClientCapabilities(request.Server?.ClientCapabilities);
      }

      // Build handlers that produce the full list from registered collections.
      // These serve as the "inner" handlers that the filtering wrappers wrap.
      McpRequestHandler<ListToolsRequestParams, ListToolsResult> listToolsInner = default!;
      McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> listPromptsInner = default!;
      McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> listResourcesInner = default!;

      // Capture the collections at configure time (they won't change afterward).
      var serverTools = options.ToolCollection?.ToList() ?? [];
      var toolList = serverTools.Select(t => t.ProtocolTool).ToList();
      var serverPrompts = options.PromptCollection?.ToList() ?? [];
      var promptList = serverPrompts.Select(p => p.ProtocolPrompt).ToList();
      var serverResources = options.ResourceCollection?.ToList() ?? [];
      var resourceList = serverResources
          .Select(r => r.ProtocolResource)
          .OfType<Resource>()
          .ToList();

      listToolsInner = (_, _) =>
          ValueTask.FromResult(new ListToolsResult { Tools = toolList });
      listPromptsInner = (_, _) =>
          ValueTask.FromResult(new ListPromptsResult { Prompts = promptList });
      listResourcesInner = (_, _) =>
          ValueTask.FromResult(new ListResourcesResult { Resources = resourceList });

      // Wrap the collection-producing handlers with capability filtering.
      // Also chain any existing user-provided handler (appended to the list).
      var existingListTools = options.Handlers.ListToolsHandler;
      var existingListPrompts = options.Handlers.ListPromptsHandler;
      var existingListResources = options.Handlers.ListResourcesHandler;

      options.Handlers.ListToolsHandler = CapabilityFilteringHandlers.WrapListTools(
          existingListTools is not null
              ? CombineHandlers(listToolsInner, existingListTools)
              : listToolsInner,
          GetClientFlags);

      options.Handlers.ListPromptsHandler = CapabilityFilteringHandlers.WrapListPrompts(
          existingListPrompts is not null
              ? CombineHandlers(listPromptsInner, existingListPrompts)
              : listPromptsInner,
          GetClientFlags);

      options.Handlers.ListResourcesHandler = CapabilityFilteringHandlers.WrapListResources(
          existingListResources is not null
              ? CombineHandlers(listResourcesInner, existingListResources)
              : listResourcesInner,
          GetClientFlags);

      // Build a CallToolHandler that dispatches to the captured server tools,
      // since clearing ToolCollection below removes the SDK's default dispatch.
      var toolLookup = serverTools.ToDictionary(t => t.ProtocolTool.Name);
      var existingCallTool = options.Handlers.CallToolHandler;

      options.Handlers.CallToolHandler = async (request, ct) =>
      {
        if (request.Params?.Name is { } toolName &&
            toolLookup.TryGetValue(toolName, out var serverTool))
        {
          return await serverTool.InvokeAsync(request, ct);
        }

        if (existingCallTool is not null)
          return await existingCallTool(request, ct);

        throw new McpProtocolException(
            $"Unknown tool: '{request.Params?.Name}'",
            McpErrorCode.InvalidParams);
      };

      // Build GetPromptHandler and ReadResourceHandler to dispatch to captured
      // server prompts/resources, since clearing the collections below removes
      // the SDK's default dispatch.
      var promptLookup = serverPrompts.ToDictionary(p => p.ProtocolPrompt.Name);
      var existingGetPrompt = options.Handlers.GetPromptHandler;

      options.Handlers.GetPromptHandler = async (request, ct) =>
      {
        if (request.Params?.Name is { } promptName &&
            promptLookup.TryGetValue(promptName, out var serverPrompt))
        {
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
          return await serverResource.ReadAsync(request, ct);
        }

        if (existingReadResource is not null)
          return await existingReadResource(request, ct);

        throw new McpProtocolException(
            $"Unknown resource URI: '{request.Params?.Uri}'",
            McpErrorCode.InvalidParams);
      };

      // Clear collections so the SDK serves only from our filtered handlers.
      options.ToolCollection = null;
      options.PromptCollection = null;
      options.ResourceCollection = null;
    }

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