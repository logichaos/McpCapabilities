namespace McpCapabilities.Server;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RequiredClientCapabilitiesAttribute : Attribute
{
    public CapabilityFlag Required { get; init; }

    public string? Message { get; init; }
}
