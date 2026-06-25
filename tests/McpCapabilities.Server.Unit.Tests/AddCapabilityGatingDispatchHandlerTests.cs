using FakeItEasy;

using McpCapabilities.Server;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class AddCapabilityGatingDispatchHandlerTests
{
  [McpServerToolType]
  private sealed class TestTools
  {
    [McpServerTool]
    public string SimpleTool(string input) => input;

    [McpServerTool]
    [RequiredClientCapabilities(Required = CapabilityFlag.Sampling)]
    public string SamplingGatedTool() => "result";
  }

  [McpServerPromptType]
  private sealed class TestPrompts
  {
    [McpServerPrompt]
    public string SimplePrompt() => "hello";
  }

  [McpServerResourceType]
  private sealed class TestResources
  {
    [McpServerResource]
    public string SimpleResource() => "content";
  }

  private static (ServiceProvider Sp, McpServerOptions Options) BuildWithGating(
      McpServerHandlers? existingHandlers = null)
  {
    var services = new ServiceCollection();
    services.AddOptions();
    services.Configure<McpServerOptions>(opt =>
    {
      opt.Handlers = existingHandlers ?? new McpServerHandlers();
      opt.ServerInfo = new Implementation { Name = "Test", Version = "1.0" };
    });

    services.AddMcpServer()
        .WithTools<TestTools>()
        .WithPrompts<TestPrompts>()
        .WithResources<TestResources>()
        .AddCapabilityGating();

    var sp = services.BuildServiceProvider();
    var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;
    return (sp, options);
  }

private static RequestContext<CallToolRequestParams> CreateCallToolContext(
      CallToolRequestParams parameters,
      ClientCapabilities? clientCapabilities = null)
  {
    var server = A.Fake<McpServer>();
    A.CallTo(() => server.ClientCapabilities).Returns(clientCapabilities);
    var request = new JsonRpcRequest { Method = "test" };
    return new RequestContext<CallToolRequestParams>(server, request, parameters);
  }

  private static RequestContext<GetPromptRequestParams> CreatePromptContext(
      GetPromptRequestParams parameters,
      ClientCapabilities? clientCapabilities = null)
  {
    var server = A.Fake<McpServer>();
    A.CallTo(() => server.ClientCapabilities).Returns(clientCapabilities);
    var request = new JsonRpcRequest { Method = "test" };
    return new RequestContext<GetPromptRequestParams>(server, request, parameters);
  }

  private static RequestContext<ReadResourceRequestParams> CreateResourceContext(
      ReadResourceRequestParams parameters,
      ClientCapabilities? clientCapabilities = null)
  {
    var server = A.Fake<McpServer>();
    A.CallTo(() => server.ClientCapabilities).Returns(clientCapabilities);
    var request = new JsonRpcRequest { Method = "test" };
    return new RequestContext<ReadResourceRequestParams>(server, request, parameters);
  }

  [Test]
  public async Task CallToolHandler_UnknownTool_ThrowsMcpProtocolException()
  {
    var (_, options) = BuildWithGating();

    var context = CreateCallToolContext(new CallToolRequestParams
    {
      Name = "nonexistent_tool",
    });

    await Assert.That(async () =>
    {
      await options.Handlers.CallToolHandler!(context, default);
    }).Throws<McpProtocolException>();
  }

  [Test]
  public async Task CallToolHandler_UnknownTool_WithExistingHandler_Delegates()
  {
    var existingCallTool = new McpRequestHandler<CallToolRequestParams, CallToolResult>(
        (_, _) => ValueTask.FromResult(new CallToolResult
        {
          Content = [new TextContentBlock { Text = "fallback" }],
        }));

    var (_, options) = BuildWithGating(existingHandlers: new McpServerHandlers
    {
      CallToolHandler = existingCallTool,
    });

    var context = CreateCallToolContext(new CallToolRequestParams
    {
      Name = "nonexistent_tool",
    });

    var result = await options.Handlers.CallToolHandler!(context, default);

    await Assert.That(result.Content).IsNotNull();
    await Assert.That(result.Content).Count().IsEqualTo(1);
  }

  [Test]
  public async Task GetPromptHandler_UnknownPrompt_ThrowsMcpProtocolException()
  {
    var (_, options) = BuildWithGating();

    var context = CreatePromptContext(new GetPromptRequestParams
    {
      Name = "nonexistent_prompt",
    });

    await Assert.That(async () =>
    {
      await options.Handlers.GetPromptHandler!(context, default);
    }).Throws<McpProtocolException>();
  }

  [Test]
  public async Task GetPromptHandler_UnknownPrompt_WithExistingHandler_Delegates()
  {
    var existingGetPrompt = new McpRequestHandler<GetPromptRequestParams, GetPromptResult>(
        (_, _) => ValueTask.FromResult(new GetPromptResult
        {
          Description = "fallback",
          Messages = [],
        }));

    var (_, options) = BuildWithGating(existingHandlers: new McpServerHandlers
    {
      GetPromptHandler = existingGetPrompt,
    });

    var context = CreatePromptContext(new GetPromptRequestParams
    {
      Name = "nonexistent_prompt",
    });

    var result = await options.Handlers.GetPromptHandler!(context, default);

    await Assert.That(result).IsNotNull();
    await Assert.That(result.Description).IsEqualTo("fallback");
  }

  [Test]
  public async Task ReadResourceHandler_UnknownResource_ThrowsMcpProtocolException()
  {
    var (_, options) = BuildWithGating();

    var context = CreateResourceContext(new ReadResourceRequestParams
    {
      Uri = "resource://nonexistent",
    });

    await Assert.That(async () =>
    {
      await options.Handlers.ReadResourceHandler!(context, default);
    }).Throws<McpProtocolException>();
  }

  [Test]
  public async Task ReadResourceHandler_UnknownResource_WithExistingHandler_Delegates()
  {
    var existingReadResource = new McpRequestHandler<ReadResourceRequestParams, ReadResourceResult>(
        (_, _) => ValueTask.FromResult(new ReadResourceResult
        {
          Contents = [new TextResourceContents { Uri = "resource://fallback", Text = "fallback" }],
        }));

    var (_, options) = BuildWithGating(existingHandlers: new McpServerHandlers
    {
      ReadResourceHandler = existingReadResource,
    });

    var context = CreateResourceContext(new ReadResourceRequestParams
    {
      Uri = "resource://nonexistent",
    });

    var result = await options.Handlers.ReadResourceHandler!(context, default);

    await Assert.That(result).IsNotNull();
    await Assert.That(result.Contents).IsNotNull();
    await Assert.That(result.Contents).Count().IsEqualTo(1);
    await Assert.That(result.Contents![0].Uri).IsEqualTo("resource://fallback");
  }

  [Test]
  public async Task CallToolHandler_GatedTool_NoCapabilities_Blocked()
  {
    var (_, options) = BuildWithGating();

    var context = CreateCallToolContext(
        new CallToolRequestParams { Name = "sampling_gated_tool" },
        clientCapabilities: null);

    await Assert.That(async () =>
    {
      await options.Handlers.CallToolHandler!(context, default);
    }).Throws<McpProtocolException>();
  }

  [Test]
  public async Task CallToolHandler_GatedTool_RequiredCapabilityPresent_Allowed()
  {
    var (_, options) = BuildWithGating();

    var context = CreateCallToolContext(
        new CallToolRequestParams { Name = "sampling_gated_tool" },
        clientCapabilities: new ClientCapabilities { Sampling = new SamplingCapability() });

    var capabilityBlocked = false;
    try
    {
      await options.Handlers.CallToolHandler!(context, default);
    }
    catch (McpProtocolException e) when (e.Message.StartsWith("Client missing"))
    {
      capabilityBlocked = true;
    }
    catch { /* tool invocation may fail with a fake server */ }

    await Assert.That(capabilityBlocked).IsFalse();
  }

  [Test]
  public async Task CallToolHandler_GatedTool_WrongCapabilityAdvertised_ThrowsMcpProtocolException()
  {
    var (_, options) = BuildWithGating();

    var context = CreateCallToolContext(
        new CallToolRequestParams { Name = "sampling_gated_tool" },
        clientCapabilities: new ClientCapabilities { Roots = new RootsCapability() });

    await Assert.That(async () =>
    {
      await options.Handlers.CallToolHandler!(context, default);
    }).Throws<McpProtocolException>();
  }
}
