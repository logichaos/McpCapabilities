using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server;

public static class CapabilityFilteringHandlers
{
    public static McpRequestHandler<ListToolsRequestParams, ListToolsResult> WrapListTools(
        McpRequestHandler<ListToolsRequestParams, ListToolsResult>? inner,
        Func<RequestContext<ListToolsRequestParams>, CapabilityFlag> getClientCapabilityFlags)
    {
        return async (request, ct) =>
        {
            var fullResult = inner is not null
                ? await inner(request, ct)
                : new ListToolsResult { Tools = [] };

            var clientFlags = getClientCapabilityFlags(request);

            var filtered = new List<Tool>(fullResult.Tools.Count);

            foreach (var tool in fullResult.Tools)
            {
                var reqs = ClientCapabilityRequirements.ReadFromMeta(tool.Meta);

                if (reqs.Required == CapabilityFlag.None)
                {
                    filtered.Add(tool);
                }
                else if ((clientFlags & reqs.Required) == reqs.Required)
                {
                    filtered.Add(tool);
                }
            }

            fullResult.Tools = filtered;
            return fullResult;
        };
    }

    public static McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> WrapListPrompts(
        McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>? inner,
        Func<RequestContext<ListPromptsRequestParams>, CapabilityFlag> getClientCapabilityFlags)
    {
        return async (request, ct) =>
        {
            var fullResult = inner is not null
                ? await inner(request, ct)
                : new ListPromptsResult { Prompts = [] };

            var clientFlags = getClientCapabilityFlags(request);

            var filtered = fullResult.Prompts
                .Where(p =>
                {
                    var reqs = ClientCapabilityRequirements.ReadFromMeta(p.Meta);
                    return reqs.Required == CapabilityFlag.None
                        || (clientFlags & reqs.Required) == reqs.Required;
                })
                .ToList();

            fullResult.Prompts = filtered;
            return fullResult;
        };
    }

    public static McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> WrapListResources(
        McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>? inner,
        Func<RequestContext<ListResourcesRequestParams>, CapabilityFlag> getClientCapabilityFlags)
    {
        return async (request, ct) =>
        {
            var fullResult = inner is not null
                ? await inner(request, ct)
                : new ListResourcesResult { Resources = [] };

            var clientFlags = getClientCapabilityFlags(request);

            var filtered = fullResult.Resources
                .Where(r =>
                {
                    var reqs = ClientCapabilityRequirements.ReadFromMeta(r.Meta);
                    return reqs.Required == CapabilityFlag.None
                        || (clientFlags & reqs.Required) == reqs.Required;
                })
                .ToList();

            fullResult.Resources = filtered;
            return fullResult;
        };
    }
}
