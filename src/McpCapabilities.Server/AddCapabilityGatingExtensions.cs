using McpCapabilities.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            var existingListTools = options.Handlers.ListToolsHandler;
            var existingListPrompts = options.Handlers.ListPromptsHandler;
            var existingListResources = options.Handlers.ListResourcesHandler;

            static CapabilityFlag GetClientFlags<TParams>(RequestContext<TParams> request)
            {
                return CapabilityFlags.FromClientCapabilities(request.Server?.ClientCapabilities);
            }

            options.Handlers.ListToolsHandler = CapabilityFilteringHandlers.WrapListTools(
                existingListTools, GetClientFlags);

            options.Handlers.ListPromptsHandler = CapabilityFilteringHandlers.WrapListPrompts(
                existingListPrompts, GetClientFlags);

            options.Handlers.ListResourcesHandler = CapabilityFilteringHandlers.WrapListResources(
                existingListResources, GetClientFlags);
        }
    }
}
