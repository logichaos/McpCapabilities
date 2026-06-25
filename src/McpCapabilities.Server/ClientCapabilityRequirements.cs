using System.Text.Json.Nodes;

using ModelContextProtocol.Protocol;

namespace McpCapabilities.Server;

public readonly record struct ClientCapabilityRequirements
{
  public static readonly ClientCapabilityRequirements None = new();

  private const string MetaKey = "__mcp_capabilities_required";

  public CapabilityFlag Required { get; init; }

  public string? Message { get; init; }

  public bool IsSatisfiedBy(ClientCapabilities? clientCaps)
      => CapabilityFlags.IsAllowed(Required, clientCaps);

  public void WriteToMeta(JsonObject meta)
  {
    meta[MetaKey] = new JsonObject
    {
      ["flags"] = Required.ToString(),
      ["message"] = Message,
    };
  }

  public static ClientCapabilityRequirements ReadFromMeta(JsonObject? meta)
  {
    if (meta is null || !meta.TryGetPropertyValue(MetaKey, out var node) || node is null)
      return None;

    var flags = CapabilityFlag.None;
    if (node["flags"]?.GetValue<string>() is { } flagsStr)
      Enum.TryParse<CapabilityFlag>(flagsStr, out flags);

    return new ClientCapabilityRequirements
    {
      Required = flags,
      Message = node["message"]?.GetValue<string>(),
    };
  }
}