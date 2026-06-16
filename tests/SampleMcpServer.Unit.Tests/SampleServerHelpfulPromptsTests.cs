using System.Reflection;

using McpCapabilities.Server;

using ModelContextProtocol.Server;

using SampleMcpServer;

namespace SampleMcpServer.Unit.Tests;

public class SampleServerHelpfulPromptsTests
{
  [Test]
  public async Task Class_HasMcpServerPromptTypeAttribute()
  {
    var attr = typeof(HelpfulPrompts).GetCustomAttribute<McpServerPromptTypeAttribute>();

    await Assert.That(attr).IsNotNull();
  }

  [Test]
  public async Task ConfirmAction_HasMcpServerPromptAttribute()
  {
    var method = typeof(HelpfulPrompts).GetMethod(nameof(HelpfulPrompts.ConfirmAction));
    var attr = method!.GetCustomAttribute<McpServerPromptAttribute>();

    await Assert.That(attr).IsNotNull();
  }

  [Test]
  public async Task ConfirmAction_HasRequiredClientCapabilities_Elicitation()
  {
    var method = typeof(HelpfulPrompts).GetMethod(nameof(HelpfulPrompts.ConfirmAction));
    var attr = method!.GetCustomAttribute<RequiredClientCapabilitiesAttribute>();

    await Assert.That(attr).IsNotNull();
    await Assert.That(attr!.Required).IsEqualTo(CapabilityFlag.Elicitation);
    await Assert.That(attr.Message).IsEqualTo("Requires user elicitation support");
  }

  [Test]
  public async Task Greeting_HasMcpServerPromptAttribute()
  {
    var method = typeof(HelpfulPrompts).GetMethod(nameof(HelpfulPrompts.Greeting));
    var attr = method!.GetCustomAttribute<McpServerPromptAttribute>();

    await Assert.That(attr).IsNotNull();
  }

  [Test]
  public async Task Greeting_DoesNotHaveRequiredClientCapabilities()
  {
    var method = typeof(HelpfulPrompts).GetMethod(nameof(HelpfulPrompts.Greeting));
    var attr = method!.GetCustomAttribute<RequiredClientCapabilitiesAttribute>();

    await Assert.That(attr).IsNull();
  }

  [Test]
  public async Task Greeting_ReturnsNonEmptyString()
  {
    var prompts = new HelpfulPrompts();

    var result = prompts.Greeting();

    await Assert.That(result).IsNotNull();
    await Assert.That(result).IsNotEmpty();
  }

  [Test]
  public async Task ConfirmAction_Method_ReturnsTaskOfStringTakesServerAndToken()
  {
    var method = typeof(HelpfulPrompts).GetMethod(nameof(HelpfulPrompts.ConfirmAction))!;

    await Assert.That(method.ReturnType).IsEqualTo(typeof(Task<string>));
    await Assert.That(method.GetParameters()).Count().IsEqualTo(2);
    await Assert.That(method.GetParameters()[0].ParameterType).IsEqualTo(typeof(McpServer));
    await Assert.That(method.GetParameters()[1].ParameterType).IsEqualTo(typeof(CancellationToken));
  }

  [Test]
  public async Task Greeting_Method_ReturnsStringTakesNoArgs()
  {
    var method = typeof(HelpfulPrompts).GetMethod(nameof(HelpfulPrompts.Greeting))!;

    await Assert.That(method.ReturnType).IsEqualTo(typeof(string));
    await Assert.That(method.GetParameters()).IsEmpty();
  }
}
