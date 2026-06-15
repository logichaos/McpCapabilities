using McpCapabilities.Server;

using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

public static class CapabilityServerBuilderExtensions
{
  public static IMcpServerBuilder WithCapabilityAwareTools<TToolType>(
      this IMcpServerBuilder builder)
      where TToolType : class
  {
    return builder.WithCapabilityAwareTools<TToolType>(configure: null);
  }

  public static IMcpServerBuilder WithCapabilityAwareTools<TToolType>(
      this IMcpServerBuilder builder,
      Action<McpServerTool, ClientCapabilityRequirements>? configure)
      where TToolType : class
  {
    builder.WithTools<TToolType>();

    builder.Services.AddSingleton<IConfigureOptions<McpServerOptions>>(
        new CapabilityCaptureConfigureOptions<TToolType>(configure));

    return builder;
  }

  private sealed class CapabilityCaptureConfigureOptions<TToolType> : IConfigureOptions<McpServerOptions>
      where TToolType : class
  {
    private readonly Action<McpServerTool, ClientCapabilityRequirements>? _configure;

    public CapabilityCaptureConfigureOptions(
        Action<McpServerTool, ClientCapabilityRequirements>? configure)
    {
      _configure = configure;
    }

    public void Configure(McpServerOptions options)
    {
      var tools = options.ToolCollection;
      if (tools is null)
        return;

      foreach (var tool in tools)
      {
        if (!ToolBelongsToType<TToolType>(tool))
          continue;

        tool.CaptureCapabilityRequirements();
        var reqs = tool.GetCapabilityRequirements();

        if (reqs.Required != CapabilityFlag.None)
        {
          _configure?.Invoke(tool, reqs);
        }
      }
    }

    private static bool ToolBelongsToType<T>(McpServerTool tool)
    {
      var methodInfo = tool.Metadata
          ?.OfType<System.Reflection.MethodInfo>()
          .FirstOrDefault();

      return methodInfo?.DeclaringType == typeof(T);
    }
  }
}