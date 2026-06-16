using System.Reflection;

using FakeItEasy;

using McpCapabilities.Server;

using ModelContextProtocol.Server;

using SampleMcpServer;

namespace SampleMcpServer.Unit.Tests;

public class SampleServerAiToolsTests
{
  [Test]
  public async Task Class_HasMcpServerToolTypeAttribute()
  {
    var attr = typeof(AiTools).GetCustomAttribute<McpServerToolTypeAttribute>();

    await Assert.That(attr).IsNotNull();
  }

  [Test]
  public async Task AiSummarize_HasMcpServerToolAttribute()
  {
    var method = typeof(AiTools).GetMethod(nameof(AiTools.AiSummarize));
    var attr = method!.GetCustomAttribute<McpServerToolAttribute>();

    await Assert.That(attr).IsNotNull();
  }

  [Test]
  public async Task AiSummarize_HasRequiredClientCapabilities_Sampling()
  {
    var method = typeof(AiTools).GetMethod(nameof(AiTools.AiSummarize));
    var attr = method!.GetCustomAttribute<RequiredClientCapabilitiesAttribute>();

    await Assert.That(attr).IsNotNull();
    await Assert.That(attr!.Required).IsEqualTo(CapabilityFlag.Sampling);
    await Assert.That(attr.Message).IsEqualTo("Requires LLM sampling support");
  }

  [Test]
  public async Task Echo_HasMcpServerToolAttribute()
  {
    var method = typeof(AiTools).GetMethod(nameof(AiTools.Echo));
    var attr = method!.GetCustomAttribute<McpServerToolAttribute>();

    await Assert.That(attr).IsNotNull();
  }

  [Test]
  public async Task Echo_DoesNotHaveRequiredClientCapabilities()
  {
    var method = typeof(AiTools).GetMethod(nameof(AiTools.Echo));
    var attr = method!.GetCustomAttribute<RequiredClientCapabilitiesAttribute>();

    await Assert.That(attr).IsNull();
  }

  [Test]
  public async Task AiSummarize_NoSamplingCapability_ReturnsFallbackMessage()
  {
    var tools = new AiTools();
    var server = A.Fake<McpServer>();
    A.CallTo(() => server.ClientCapabilities).Returns(null);
    using var cts = new CancellationTokenSource();

    var result = await tools.AiSummarize(server, "hello", cts.Token);

    await Assert.That(result).IsEqualTo("Client does not support sampling.");
  }

  [Test]
  public async Task Echo_ReturnsInputVerbatim()
  {
    var tools = new AiTools();

    var result = tools.Echo("test input");

    await Assert.That(result).IsEqualTo("test input");
  }

  [Test]
  public async Task AiSummarize_Method_IsReflectedCorrectly()
  {
    var method = typeof(AiTools).GetMethod(nameof(AiTools.AiSummarize))!;

    await Assert.That(method.ReturnType).IsEqualTo(typeof(Task<string>));
    await Assert.That(method.GetParameters()).Count().IsEqualTo(3);
    await Assert.That(method.GetParameters()[0].ParameterType).IsEqualTo(typeof(McpServer));
    await Assert.That(method.GetParameters()[1].ParameterType).IsEqualTo(typeof(string));
    await Assert.That(method.GetParameters()[2].ParameterType).IsEqualTo(typeof(CancellationToken));
  }

  [Test]
  public async Task Echo_Method_IsReflectedCorrectly()
  {
    var method = typeof(AiTools).GetMethod(nameof(AiTools.Echo))!;

    await Assert.That(method.ReturnType).IsEqualTo(typeof(string));
    await Assert.That(method.GetParameters()).Count().IsEqualTo(1);
    await Assert.That(method.GetParameters()[0].ParameterType).IsEqualTo(typeof(string));
  }
}