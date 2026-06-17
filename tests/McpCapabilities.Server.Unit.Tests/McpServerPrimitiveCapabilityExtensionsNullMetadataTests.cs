using System.Reflection;
using System.Text.Json.Nodes;

using McpCapabilities.Server;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class McpServerPrimitiveCapabilityExtensionsNullMetadataTests
{
  private sealed class NullMetadataTool : McpServerTool
  {
    public override IReadOnlyList<object> Metadata => null!;
    public override Tool ProtocolTool => new() { Name = "null_metadata_tool" };
    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request, CancellationToken ct)
        => default;
  }

  private sealed class NullMetadataPrompt : McpServerPrompt
  {
    public override IReadOnlyList<object> Metadata => null!;
    public override Prompt ProtocolPrompt => new() { Name = "null_metadata_prompt" };
    public override ValueTask<GetPromptResult> GetAsync(
        RequestContext<GetPromptRequestParams> request, CancellationToken ct)
        => default;
  }

  private sealed class NullMetadataResource : McpServerResource
  {
    public override IReadOnlyList<object> Metadata => null!;
    public override Resource ProtocolResource => new()
    {
      Name = "null_metadata_resource",
      Uri = "resource://null",
    };
    public override ResourceTemplate ProtocolResourceTemplate => new()
    {
      UriTemplate = "resource://null",
      Name = "null_metadata_resource",
    };
    public override ValueTask<ReadResourceResult> ReadAsync(
        RequestContext<ReadResourceRequestParams> request, CancellationToken ct)
        => default;
    public override bool IsMatch(string uri) => uri == "resource://null";
  }

  [Test]
  public async Task CaptureCapabilityRequirements_ToolNullMetadata_DoesNotThrow()
  {
    var tool = new NullMetadataTool();
    tool.CaptureCapabilityRequirements();
    // If the method threw, the test would fail — no assertion needed.
    await Task.CompletedTask;
  }

  [Test]
  public async Task CaptureCapabilityRequirements_PromptNullMetadata_DoesNotThrow()
  {
    var prompt = new NullMetadataPrompt();
    prompt.CaptureCapabilityRequirements();
    await Task.CompletedTask;
  }

  [Test]
  public async Task CaptureCapabilityRequirements_ResourceNullMetadata_DoesNotThrow()
  {
    var resource = new NullMetadataResource();
    resource.CaptureCapabilityRequirements();
    await Task.CompletedTask;
  }
}
