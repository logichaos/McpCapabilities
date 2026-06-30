using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server;

public static class CapabilityFilteringHandlers
{
  public static McpRequestHandler<ListToolsRequestParams, ListToolsResult> WrapListTools(
      McpRequestHandler<ListToolsRequestParams, ListToolsResult>? inner,
      Func<RequestContext<ListToolsRequestParams>, ClientCapabilities?> getClientCapabilities,
      bool allowWhenClientCapabilitiesNotProvided = true,
      ILogger? logger = null)
  {
    return async (request, ct) =>
    {
      using var activity = McpCapabilitiesTelemetry.Source.StartActivity("filter tools/list");
      activity?.SetTag(McpCapabilitiesTelemetry.Tags.PrimitiveType, "tool");

      var fullResult = inner is not null
              ? await inner(request, ct)
              : new ListToolsResult { Tools = [] };

      var clientCaps = getClientCapabilities(request);
      var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
      activity?.SetTag(McpCapabilitiesTelemetry.Tags.ClientFlags, clientFlags.ToString());

      logger?.LogDebug("Filtering tools list. ClientCapabilities: {ClientFlags}", clientFlags);

      var filtered = new List<Tool>(fullResult.Tools.Count);

      foreach (var tool in fullResult.Tools)
      {
        var reqs = ClientCapabilityRequirements.ReadFromMeta(tool.Meta);
        var allowed = CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allowWhenClientCapabilitiesNotProvided);
        if (allowed)
        {
          filtered.Add(tool);
          if (reqs.Required != CapabilityFlag.None)
            logger?.LogDebug("Tool '{ToolName}' included (requires {Required}, client has {ClientFlags})", tool.Name, reqs.Required, clientFlags);
        }
        else
        {
          logger?.LogDebug("Tool '{ToolName}' excluded (requires {Required}, client missing {Missing})", tool.Name, reqs.Required, reqs.Required & ~clientFlags);
        }

        if (reqs.Required != CapabilityFlag.None)
        {
          activity?.AddEvent(new ActivityEvent(
              allowed ? "tool_included" : "tool_excluded",
              tags: new ActivityTagsCollection
              {
                [McpCapabilitiesTelemetry.Tags.PrimitiveName] = tool.Name,
                [McpCapabilitiesTelemetry.Tags.RequiredFlags] = reqs.Required.ToString(),
                [McpCapabilitiesTelemetry.Tags.ClientFlags] = clientFlags.ToString(),
                [McpCapabilitiesTelemetry.Tags.Allowed] = allowed,
              }));
        }
      }

      activity?.SetTag(McpCapabilitiesTelemetry.Tags.VisibleCount, filtered.Count);
      activity?.SetTag(McpCapabilitiesTelemetry.Tags.TotalCount, fullResult.Tools.Count);

      logger?.LogInformation("Tools list: {Visible} of {Total} visible to client", filtered.Count, fullResult.Tools.Count);

      fullResult.Tools = filtered;
      return fullResult;
    };
  }

  public static McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> WrapListPrompts(
      McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>? inner,
      Func<RequestContext<ListPromptsRequestParams>, ClientCapabilities?> getClientCapabilities,
      bool allowWhenClientCapabilitiesNotProvided = true,
      ILogger? logger = null)
  {
    return async (request, ct) =>
    {
      using var activity = McpCapabilitiesTelemetry.Source.StartActivity("filter prompts/list");
      activity?.SetTag(McpCapabilitiesTelemetry.Tags.PrimitiveType, "prompt");

      var fullResult = inner is not null
              ? await inner(request, ct)
              : new ListPromptsResult { Prompts = [] };

      var clientCaps = getClientCapabilities(request);
      var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
      activity?.SetTag(McpCapabilitiesTelemetry.Tags.ClientFlags, clientFlags.ToString());

      logger?.LogDebug("Filtering prompts list. ClientCapabilities: {ClientFlags}", clientFlags);

      var filtered = new List<Prompt>(fullResult.Prompts.Count);

      foreach (var prompt in fullResult.Prompts)
      {
        var reqs = ClientCapabilityRequirements.ReadFromMeta(prompt.Meta);
        var allowed = CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allowWhenClientCapabilitiesNotProvided);
        if (allowed)
        {
          filtered.Add(prompt);
          if (reqs.Required != CapabilityFlag.None)
            logger?.LogDebug("Prompt '{PromptName}' included (requires {Required}, client has {ClientFlags})", prompt.Name, reqs.Required, clientFlags);
        }
        else
        {
          logger?.LogDebug("Prompt '{PromptName}' excluded (requires {Required}, client missing {Missing})", prompt.Name, reqs.Required, reqs.Required & ~clientFlags);
        }

        if (reqs.Required != CapabilityFlag.None)
        {
          activity?.AddEvent(new ActivityEvent(
              allowed ? "prompt_included" : "prompt_excluded",
              tags: new ActivityTagsCollection
              {
                [McpCapabilitiesTelemetry.Tags.PrimitiveName] = prompt.Name,
                [McpCapabilitiesTelemetry.Tags.RequiredFlags] = reqs.Required.ToString(),
                [McpCapabilitiesTelemetry.Tags.ClientFlags] = clientFlags.ToString(),
                [McpCapabilitiesTelemetry.Tags.Allowed] = allowed,
              }));
        }
      }

      activity?.SetTag(McpCapabilitiesTelemetry.Tags.VisibleCount, filtered.Count);
      activity?.SetTag(McpCapabilitiesTelemetry.Tags.TotalCount, fullResult.Prompts.Count);

      logger?.LogInformation("Prompts list: {Visible} of {Total} visible to client", filtered.Count, fullResult.Prompts.Count);

      fullResult.Prompts = filtered;
      return fullResult;
    };
  }

  public static McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> WrapListResources(
      McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>? inner,
      Func<RequestContext<ListResourcesRequestParams>, ClientCapabilities?> getClientCapabilities,
      bool allowWhenClientCapabilitiesNotProvided = true,
      ILogger? logger = null)
  {
    return async (request, ct) =>
    {
      using var activity = McpCapabilitiesTelemetry.Source.StartActivity("filter resources/list");
      activity?.SetTag(McpCapabilitiesTelemetry.Tags.PrimitiveType, "resource");

      var fullResult = inner is not null
              ? await inner(request, ct)
              : new ListResourcesResult { Resources = [] };

      var clientCaps = getClientCapabilities(request);
      var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
      activity?.SetTag(McpCapabilitiesTelemetry.Tags.ClientFlags, clientFlags.ToString());

      logger?.LogDebug("Filtering resources list. ClientCapabilities: {ClientFlags}", clientFlags);

      var filtered = new List<Resource>(fullResult.Resources.Count);

      foreach (var resource in fullResult.Resources)
      {
        var reqs = ClientCapabilityRequirements.ReadFromMeta(resource.Meta);
        var allowed = CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allowWhenClientCapabilitiesNotProvided);
        if (allowed)
        {
          filtered.Add(resource);
          if (reqs.Required != CapabilityFlag.None)
            logger?.LogDebug("Resource '{ResourceName}' included (requires {Required}, client has {ClientFlags})", resource.Name, reqs.Required, clientFlags);
        }
        else
        {
          logger?.LogDebug("Resource '{ResourceName}' excluded (requires {Required}, client missing {Missing})", resource.Name, reqs.Required, reqs.Required & ~clientFlags);
        }

        if (reqs.Required != CapabilityFlag.None)
        {
          activity?.AddEvent(new ActivityEvent(
              allowed ? "resource_included" : "resource_excluded",
              tags: new ActivityTagsCollection
              {
                [McpCapabilitiesTelemetry.Tags.PrimitiveName] = resource.Name ?? resource.Uri ?? "",
                [McpCapabilitiesTelemetry.Tags.RequiredFlags] = reqs.Required.ToString(),
                [McpCapabilitiesTelemetry.Tags.ClientFlags] = clientFlags.ToString(),
                [McpCapabilitiesTelemetry.Tags.Allowed] = allowed,
              }));
        }
      }

      activity?.SetTag(McpCapabilitiesTelemetry.Tags.VisibleCount, filtered.Count);
      activity?.SetTag(McpCapabilitiesTelemetry.Tags.TotalCount, fullResult.Resources.Count);

      logger?.LogInformation("Resources list: {Visible} of {Total} visible to client", filtered.Count, fullResult.Resources.Count);

      fullResult.Resources = filtered;
      return fullResult;
    };
  }
}
