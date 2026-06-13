using System.Text.Json.Nodes;
using McpCapabilities.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SampleMcpServer;

namespace SampleMcpServer.Integration.Tests;

public class SampleMcpServerIntegrationTests
{
    private static IServiceProvider BuildSampleServer(Action<McpServerOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddOptions();

        services.AddMcpServer(options =>
        {
            options.ServerInfo = new Implementation { Name = "SampleMcpServer", Version = "1.0" };
            options.Handlers = new McpServerHandlers();
            configureOptions?.Invoke(options);
        })
            .WithCapabilityAwareTools<AiTools>()
            .WithPrompts<HelpfulPrompts>()
            .WithResources<WorkspaceResources>()
            .AddCapabilityGating();

        services.AddSingleton<IConfigureOptions<McpServerOptions>>(
            new PromptResourceCapabilityCapture());

        return services.BuildServiceProvider();
    }

    [Test]
    public async Task AiSummarizeTool_HasCapabilityRequirementsCaptured()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        await Assert.That(options.ToolCollection).IsNotNull();
        var aiSummarize = options.ToolCollection!.FirstOrDefault(t =>
            t.ProtocolTool.Name == "ai_summarize");

        await Assert.That(aiSummarize).IsNotNull();
        var reqs = aiSummarize!.GetCapabilityRequirements();
        await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Sampling);
        await Assert.That(reqs.Message).IsEqualTo("Requires LLM sampling support");
    }

    [Test]
    public async Task EchoTool_HasNoCapabilityRequirements()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var echo = options.ToolCollection!.FirstOrDefault(t =>
            t.ProtocolTool.Name == "echo");

        await Assert.That(echo).IsNotNull();
        var reqs = echo!.GetCapabilityRequirements();
        await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.None);
    }

    [Test]
    public async Task ConfirmActionPrompt_HasCapabilityRequirementsCaptured()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        await Assert.That(options.PromptCollection).IsNotNull();
        var confirmAction = options.PromptCollection!.FirstOrDefault(p =>
            p.ProtocolPrompt.Name == "confirm_action");

        await Assert.That(confirmAction).IsNotNull();
        var reqs = confirmAction!.GetCapabilityRequirements();
        await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Elicitation);
        await Assert.That(reqs.Message).IsEqualTo("Requires user elicitation support");
    }

    [Test]
    public async Task GreetingPrompt_HasNoCapabilityRequirements()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var greeting = options.PromptCollection!.FirstOrDefault(p =>
            p.ProtocolPrompt.Name == "greeting");

        await Assert.That(greeting).IsNotNull();
        var reqs = greeting!.GetCapabilityRequirements();
        await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.None);
    }

    [Test]
    public async Task WorkspaceFilesResource_HasCapabilityRequirementsCaptured()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        await Assert.That(options.ResourceCollection).IsNotNull();
        var workspaceFiles = options.ResourceCollection!.FirstOrDefault(r =>
            r.ProtocolResource?.Name == "workspace_files");

        await Assert.That(workspaceFiles).IsNotNull();
        var reqs = workspaceFiles!.GetCapabilityRequirements();
        await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Roots);
        await Assert.That(reqs.Message).IsEqualTo("Requires filesystem root listing support");
    }

    [Test]
    public async Task AppInfoResource_HasNoCapabilityRequirements()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var appInfo = options.ResourceCollection!.FirstOrDefault(r =>
            r.ProtocolResource?.Name == "app_info");

        await Assert.That(appInfo).IsNotNull();
        var reqs = appInfo!.GetCapabilityRequirements();
        await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.None);
    }

    [Test]
    public async Task Tools_FilteredByClientCapabilities_SamplingClientSeesBoth()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var tools = options.ToolCollection!.ToList()!;
        var protools = tools.Select(t => t.ProtocolTool).ToList();

        var clientCaps = new ClientCapabilities
        {
            Sampling = new SamplingCapability(),
        };

        var result = protools.FilterByClientCapabilities(clientCaps);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Tools_FilteredByClientCapabilities_NoClientHidesAiSummarize()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var tools = options.ToolCollection!.ToList()!;
        var protools = tools.Select(t => t.ProtocolTool).ToList();
        var clientCaps = new ClientCapabilities();

        var result = protools.FilterByClientCapabilities(clientCaps);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        await Assert.That(result.Value[0].Name).IsEqualTo("echo");
    }

    [Test]
    public async Task Prompts_FilteredByClientCapabilities_ElicitationClientSeesBoth()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var prompts = options.PromptCollection!
            .Select(p => p.ProtocolPrompt)
            .ToList();
        var clientCaps = new ClientCapabilities
        {
            Elicitation = new ElicitationCapability(),
        };

        var result = prompts.FilterByClientCapabilities(clientCaps);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Prompts_FilteredByClientCapabilities_NoClientHidesConfirmAction()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var prompts = options.PromptCollection!
            .Select(p => p.ProtocolPrompt)
            .ToList();
        var clientCaps = new ClientCapabilities();

        var result = prompts.FilterByClientCapabilities(clientCaps);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        await Assert.That(result.Value[0].Name).IsEqualTo("greeting");
    }

    [Test]
    public async Task Resources_FilteredByClientCapabilities_RootsClientSeesBoth()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var resources = options.ResourceCollection!
            .Select(r => r.ProtocolResource)
            .OfType<Resource>()
            .ToList();
        var clientCaps = new ClientCapabilities
        {
            Roots = new RootsCapability(),
        };

        var result = resources.FilterByClientCapabilities(clientCaps);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Resources_FilteredByClientCapabilities_NoClientHidesWorkspaceFiles()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var resources = options.ResourceCollection!
            .Select(r => r.ProtocolResource)
            .OfType<Resource>()
            .ToList();
        var clientCaps = new ClientCapabilities();

        var result = resources.FilterByClientCapabilities(clientCaps);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        await Assert.That(result.Value[0].Name).IsEqualTo("app_info");
    }

    [Test]
    public async Task PromptResourceCapabilityCapture_DoesNotThrow_OnEmptyCollections()
    {
        var capture = new PromptResourceCapabilityCapture();
        var options = new McpServerOptions();

        // Should not throw when collections are null
        capture.Configure(options);
    }

    [Test]
    public async Task AllPrimitives_RegisteredCountsAreCorrect()
    {
        var sp = BuildSampleServer();
        var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        await Assert.That(options.ToolCollection).IsNotNull();
        await Assert.That(options.ToolCollection!).Count().IsEqualTo(2);

        await Assert.That(options.PromptCollection).IsNotNull();
        await Assert.That(options.PromptCollection!).Count().IsEqualTo(2);

        await Assert.That(options.ResourceCollection).IsNotNull();
        await Assert.That(options.ResourceCollection!).Count().IsEqualTo(2);
    }
}
