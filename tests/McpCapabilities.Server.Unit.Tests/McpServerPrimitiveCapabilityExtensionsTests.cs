using System.Reflection;
using System.Text.Json.Nodes;

using McpCapabilities.Server;

using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class McpServerPrimitiveCapabilityExtensionsTests
{
  public class AnnotatedToolClass
  {
    [RequiredClientCapabilities(Required = CapabilityFlag.Sampling, Message = "Needs LLM")]
    public string ToolWithCapability(string input) => input;

    public string ToolWithoutCapability(string input) => input;
  }

  public class AnnotatedPromptClass
  {
    [RequiredClientCapabilities(Required = CapabilityFlag.Elicitation)]
    public string PromptWithCapability() => "prompt";

    public string PromptWithoutCapability() => "prompt";
  }

  public class AnnotatedResourceClass
  {
    [RequiredClientCapabilities(Required = CapabilityFlag.Roots)]
    public string ResourceWithCapability() => "resource";

    public string ResourceWithoutCapability() => "resource";
  }

  [Test]
  public async Task CaptureCapabilityRequirements_ToolWithAttribute_WritesToMeta()
  {
    var method = typeof(AnnotatedToolClass).GetMethod(nameof(AnnotatedToolClass.ToolWithCapability))!;
    var tool = McpServerTool.Create(method, new AnnotatedToolClass());

    tool.CaptureCapabilityRequirements();

    var meta = tool.ProtocolTool.Meta;
    await Assert.That(meta).IsNotNull();
    var capNode = meta!["__mcp_capabilities_required"];
    await Assert.That(capNode).IsNotNull();
    await Assert.That(capNode!["flags"]!.GetValue<string>()).IsEqualTo("Sampling");
    await Assert.That(capNode["message"]!.GetValue<string>()).IsEqualTo("Needs LLM");
  }

  [Test]
  public async Task CaptureCapabilityRequirements_ToolWithoutAttribute_DoesNotWriteMeta()
  {
    var method = typeof(AnnotatedToolClass).GetMethod(nameof(AnnotatedToolClass.ToolWithoutCapability))!;
    var tool = McpServerTool.Create(method, new AnnotatedToolClass());

    tool.CaptureCapabilityRequirements();

    var meta = tool.ProtocolTool.Meta;
    if (meta is not null)
    {
      await Assert.That(meta.ContainsKey("__mcp_capabilities_required")).IsFalse();
    }
  }

  [Test]
  public async Task GetCapabilityRequirements_AfterCapture_ReturnsCorrectRequirements()
  {
    var method = typeof(AnnotatedToolClass).GetMethod(nameof(AnnotatedToolClass.ToolWithCapability))!;
    var tool = McpServerTool.Create(method, new AnnotatedToolClass());

    tool.CaptureCapabilityRequirements();

    var reqs = tool.GetCapabilityRequirements();
    await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Sampling);
    await Assert.That(reqs.Message).IsEqualTo("Needs LLM");
  }

  [Test]
  public async Task GetCapabilityRequirements_WithoutCapture_ReturnsNone()
  {
    var method = typeof(AnnotatedToolClass).GetMethod(nameof(AnnotatedToolClass.ToolWithoutCapability))!;
    var tool = McpServerTool.Create(method, new AnnotatedToolClass());

    var reqs = tool.GetCapabilityRequirements();

    await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.None);
  }

  [Test]
  public async Task CaptureCapabilityRequirements_PromptWithAttribute_WritesToMeta()
  {
    var method = typeof(AnnotatedPromptClass).GetMethod(nameof(AnnotatedPromptClass.PromptWithCapability))!;
    var prompt = McpServerPrompt.Create(method, new AnnotatedPromptClass());

    prompt.CaptureCapabilityRequirements();

    var meta = prompt.ProtocolPrompt.Meta;
    await Assert.That(meta).IsNotNull();
    var capNode = meta!["__mcp_capabilities_required"];
    await Assert.That(capNode).IsNotNull();
    await Assert.That(capNode!["flags"]!.GetValue<string>()).IsEqualTo("Elicitation");
  }

  [Test]
  public async Task CaptureCapabilityRequirements_PromptWithoutAttribute_DoesNotWriteMeta()
  {
    var method = typeof(AnnotatedPromptClass).GetMethod(nameof(AnnotatedPromptClass.PromptWithoutCapability))!;
    var prompt = McpServerPrompt.Create(method, new AnnotatedPromptClass());

    prompt.CaptureCapabilityRequirements();

    var meta = prompt.ProtocolPrompt.Meta;
    if (meta is not null)
    {
      await Assert.That(meta.ContainsKey("__mcp_capabilities_required")).IsFalse();
    }
  }

  [Test]
  public async Task GetCapabilityRequirements_Prompt_AfterCapture_ReturnsCorrectRequirements()
  {
    var method = typeof(AnnotatedPromptClass).GetMethod(nameof(AnnotatedPromptClass.PromptWithCapability))!;
    var prompt = McpServerPrompt.Create(method, new AnnotatedPromptClass());

    prompt.CaptureCapabilityRequirements();

    var reqs = prompt.GetCapabilityRequirements();
    await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Elicitation);
  }

  [Test]
  public async Task CaptureCapabilityRequirements_ResourceWithAttribute_WritesToMeta()
  {
    var method = typeof(AnnotatedResourceClass).GetMethod(nameof(AnnotatedResourceClass.ResourceWithCapability))!;
    var resource = McpServerResource.Create(method, new AnnotatedResourceClass());

    resource.CaptureCapabilityRequirements();

    var protocolResource = resource.ProtocolResource;
    await Assert.That(protocolResource).IsNotNull();
    var meta = protocolResource!.Meta;
    await Assert.That(meta).IsNotNull();
    var capNode = meta!["__mcp_capabilities_required"];
    await Assert.That(capNode).IsNotNull();
    await Assert.That(capNode!["flags"]!.GetValue<string>()).IsEqualTo("Roots");
  }

  [Test]
  public async Task CaptureCapabilityRequirements_ResourceWithoutAttribute_DoesNotWriteMeta()
  {
    var method = typeof(AnnotatedResourceClass).GetMethod(nameof(AnnotatedResourceClass.ResourceWithoutCapability))!;
    var resource = McpServerResource.Create(method, new AnnotatedResourceClass());

    resource.CaptureCapabilityRequirements();

    var protocolResource = resource.ProtocolResource;
    if (protocolResource?.Meta is not null)
    {
      await Assert.That(protocolResource.Meta.ContainsKey("__mcp_capabilities_required")).IsFalse();
    }
  }

  [Test]
  public async Task GetCapabilityRequirements_Resource_AfterCapture_ReturnsCorrectRequirements()
  {
    var method = typeof(AnnotatedResourceClass).GetMethod(nameof(AnnotatedResourceClass.ResourceWithCapability))!;
    var resource = McpServerResource.Create(method, new AnnotatedResourceClass());

    resource.CaptureCapabilityRequirements();

    var reqs = resource.GetCapabilityRequirements();
    await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Roots);
  }
}