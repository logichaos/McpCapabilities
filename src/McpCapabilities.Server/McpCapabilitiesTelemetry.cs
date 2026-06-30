using System.Diagnostics;

namespace McpCapabilities.Server;

internal static class McpCapabilitiesTelemetry
{
  public const string SourceName = "McpCapabilities.Server";

  public static readonly ActivitySource Source = new(SourceName, "1.0.0");

  public static class Tags
  {
    public const string PrimitiveType = "mcp.capabilities.primitive_type";
    public const string PrimitiveName = "mcp.capabilities.primitive_name";
    public const string ClientFlags = "mcp.capabilities.client_flags";
    public const string RequiredFlags = "mcp.capabilities.required_flags";
    public const string MissingFlags = "mcp.capabilities.missing_flags";
    public const string Allowed = "mcp.capabilities.allowed";
    public const string VisibleCount = "mcp.capabilities.visible_count";
    public const string TotalCount = "mcp.capabilities.total_count";
  }
}
