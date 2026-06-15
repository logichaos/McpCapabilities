using FluentResults;

namespace McpCapabilities.Server;

public class CapabilityNotMetError : Error
{
  public CapabilityFlag Required { get; }

  public CapabilityFlag Missing { get; }

  public string PrimitiveName { get; }

  public CapabilityNotMetError(
      CapabilityFlag required,
      CapabilityFlag missing,
      string primitiveName,
      string? message = null)
      : base(message ?? BuildDefaultMessage(required, missing, primitiveName))
  {
    Required = required;
    Missing = missing;
    PrimitiveName = primitiveName;

    WithMetadata("RequiredFlags", required.ToString());
    WithMetadata("MissingFlags", missing.ToString());
    WithMetadata("PrimitiveName", primitiveName);
  }

  private static string BuildDefaultMessage(
      CapabilityFlag required,
      CapabilityFlag missing,
      string primitiveName)
      => $"'{primitiveName}' requires {required}. Missing: {missing}";
}