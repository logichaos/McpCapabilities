using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server;

public static class CapabilityFilteringHandlers
{
  public static McpRequestHandler<ListToolsRequestParams, ListToolsResult> WrapListTools(
      McpRequestHandler<ListToolsRequestParams, ListToolsResult>? inner,
      Func<RequestContext<ListToolsRequestParams>, ClientCapabilities?> getClientCapabilities,
      bool allowWhenClientCapabilitiesNotProvided = true)
  {
    return async (request, ct) =>
    {
      var fullResult = inner is not null
              ? await inner(request, ct)
              : new ListToolsResult { Tools = [] };

      var clientCaps = getClientCapabilities(request);

      var filtered = new List<Tool>(fullResult.Tools.Count);

      foreach (var tool in fullResult.Tools)
      {
        var reqs = ClientCapabilityRequirements.ReadFromMeta(tool.Meta);
        if (CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allowWhenClientCapabilitiesNotProvided))
          filtered.Add(tool);
      }

      fullResult.Tools = filtered;
      return fullResult;
    };
  }

  public static McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> WrapListPrompts(
      McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>? inner,
      Func<RequestContext<ListPromptsRequestParams>, ClientCapabilities?> getClientCapabilities,
      bool allowWhenClientCapabilitiesNotProvided = true)
  {
    return async (request, ct) =>
    {
      var fullResult = inner is not null
              ? await inner(request, ct)
              : new ListPromptsResult { Prompts = [] };

      var clientCaps = getClientCapabilities(request);

      var filtered = fullResult.Prompts
              .Where(p =>
              {
                var reqs = ClientCapabilityRequirements.ReadFromMeta(p.Meta);
                return CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allowWhenClientCapabilitiesNotProvided);
              })
              .ToList();

      fullResult.Prompts = filtered;
      return fullResult;
    };
  }

  public static McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> WrapListResources(
      McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>? inner,
      Func<RequestContext<ListResourcesRequestParams>, ClientCapabilities?> getClientCapabilities,
      bool allowWhenClientCapabilitiesNotProvided = true)
  {
    return async (request, ct) =>
    {
      var fullResult = inner is not null
              ? await inner(request, ct)
              : new ListResourcesResult { Resources = [] };

      var clientCaps = getClientCapabilities(request);

      var filtered = fullResult.Resources
              .Where(r =>
              {
                var reqs = ClientCapabilityRequirements.ReadFromMeta(r.Meta);
                return CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allowWhenClientCapabilitiesNotProvided);
              })
              .ToList();

      fullResult.Resources = filtered;
      return fullResult;
    };
  }
}
