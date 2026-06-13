using System.Reflection;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server;

public static class McpServerPrimitiveCapabilityExtensions
{
    public static void CaptureCapabilityRequirements(this McpServerTool tool)
    {
        var methodInfo = tool.Metadata
            ?.OfType<MethodInfo>()
            .FirstOrDefault();

        if (methodInfo is null)
            return;

        var attr = methodInfo.GetCustomAttribute<RequiredClientCapabilitiesAttribute>();
        if (attr is null)
            return;

        var reqs = new ClientCapabilityRequirements
        {
            Required = attr.Required,
            Message = attr.Message,
        };

        tool.ProtocolTool.Meta ??= [];
        reqs.WriteToMeta(tool.ProtocolTool.Meta);
    }

    public static ClientCapabilityRequirements GetCapabilityRequirements(this McpServerTool tool)
        => ClientCapabilityRequirements.ReadFromMeta(tool.ProtocolTool.Meta);

    public static void CaptureCapabilityRequirements(this McpServerPrompt prompt)
    {
        var methodInfo = prompt.Metadata
            ?.OfType<MethodInfo>()
            .FirstOrDefault();

        if (methodInfo is null)
            return;

        var attr = methodInfo.GetCustomAttribute<RequiredClientCapabilitiesAttribute>();
        if (attr is null)
            return;

        var reqs = new ClientCapabilityRequirements
        {
            Required = attr.Required,
            Message = attr.Message,
        };

        prompt.ProtocolPrompt.Meta ??= [];
        reqs.WriteToMeta(prompt.ProtocolPrompt.Meta);
    }

    public static ClientCapabilityRequirements GetCapabilityRequirements(this McpServerPrompt prompt)
        => ClientCapabilityRequirements.ReadFromMeta(prompt.ProtocolPrompt.Meta);

    public static void CaptureCapabilityRequirements(this McpServerResource resource)
    {
        var methodInfo = resource.Metadata
            ?.OfType<MethodInfo>()
            .FirstOrDefault();

        if (methodInfo is null)
            return;

        var attr = methodInfo.GetCustomAttribute<RequiredClientCapabilitiesAttribute>();
        if (attr is null)
            return;

        var reqs = new ClientCapabilityRequirements
        {
            Required = attr.Required,
            Message = attr.Message,
        };

        var protocolResource = resource.ProtocolResource;
        if (protocolResource is not null)
        {
            protocolResource.Meta ??= [];
            reqs.WriteToMeta(protocolResource.Meta);
        }
    }

    public static ClientCapabilityRequirements GetCapabilityRequirements(this McpServerResource resource)
        => ClientCapabilityRequirements.ReadFromMeta(resource.ProtocolResource?.Meta);
}
