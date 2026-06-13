using McpCapabilities.Server;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using SampleMcpServer;

namespace SampleMcpHttpClient.Integration.Tests;

public class SampleMcpHttpClientIntegrationTests : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly string _serverUrl;

    public SampleMcpHttpClientIntegrationTests()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = "SampleMcpServer",
        });

        var config = new Dictionary<string, string?>
        {
            ["MCP:Transport"] = "http",
        };
        builder.Configuration.AddInMemoryCollection(config);
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddMcpServer(options =>
        {
            options.ServerInfo = new Implementation { Name = "SampleMcpServer", Version = "1.0" };
            options.Handlers = new ModelContextProtocol.Server.McpServerHandlers();
        })
            .WithCapabilityAwareTools<AiTools>()
            .WithPrompts<HelpfulPrompts>()
            .WithResources<WorkspaceResources>()
            .AddCapabilityGating()
            .WithHttpTransport();

        _app = builder.Build();
        _app.MapMcp();

        _app.StartAsync().GetAwaiter().GetResult();

        var address = _app.Urls.First();
        _serverUrl = address;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private async Task<McpClient> ConnectClient(ClientCapabilities capabilities)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_serverUrl),
        });

        return await McpClient.CreateAsync(transport, new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "TestHttpClient", Version = "1.0" },
            Capabilities = capabilities,
        });
    }

    [Test]
    public async Task FullCapabilityClient_OverHttp_SeesAllTools()
    {
        await using var client = await ConnectClient(new ClientCapabilities
        {
            Sampling = new SamplingCapability(),
            Roots = new RootsCapability(),
            Elicitation = new ElicitationCapability(),
        });

        var tools = await client.ListToolsAsync();

        await Assert.That(tools.Count).IsEqualTo(2);
        await Assert.That(tools.Select(t => t.Name).ToList())
            .IsEquivalentTo(["ai_summarize", "echo"]);
    }

    [Test]
    public async Task FullCapabilityClient_OverHttp_SeesAllPrompts()
    {
        await using var client = await ConnectClient(new ClientCapabilities
        {
            Sampling = new SamplingCapability(),
            Roots = new RootsCapability(),
            Elicitation = new ElicitationCapability(),
        });

        var prompts = await client.ListPromptsAsync();

        await Assert.That(prompts.Count).IsEqualTo(2);
        await Assert.That(prompts.Select(p => p.Name).ToList())
            .IsEquivalentTo(["confirm_action", "greeting"]);
    }

    [Test]
    public async Task FullCapabilityClient_OverHttp_SeesAllResources()
    {
        await using var client = await ConnectClient(new ClientCapabilities
        {
            Sampling = new SamplingCapability(),
            Roots = new RootsCapability(),
            Elicitation = new ElicitationCapability(),
        });

        var resources = await client.ListResourcesAsync();

        await Assert.That(resources.Count).IsEqualTo(2);
        await Assert.That(resources.Select(r => r.Name).ToList())
            .IsEquivalentTo(["workspace_files", "app_info"]);
    }

    [Test]
    public async Task MinimalCapabilityClient_OverHttp_SeesOnlyUngatedTools()
    {
        await using var client = await ConnectClient(new ClientCapabilities());

        var tools = await client.ListToolsAsync();

        await Assert.That(tools.Count).IsEqualTo(1);
        await Assert.That(tools[0].Name).IsEqualTo("echo");
    }

    [Test]
    public async Task MinimalCapabilityClient_OverHttp_SeesOnlyUngatedPrompts()
    {
        await using var client = await ConnectClient(new ClientCapabilities());

        var prompts = await client.ListPromptsAsync();

        await Assert.That(prompts.Count).IsEqualTo(1);
        await Assert.That(prompts[0].Name).IsEqualTo("greeting");
    }

    [Test]
    public async Task MinimalCapabilityClient_OverHttp_SeesOnlyUngatedResources()
    {
        await using var client = await ConnectClient(new ClientCapabilities());

        var resources = await client.ListResourcesAsync();

        await Assert.That(resources.Count).IsEqualTo(1);
        await Assert.That(resources[0].Name).IsEqualTo("app_info");
    }

    [Test]
    public async Task FullCapabilityClient_OverHttp_DoesNotContainGatedToolsInMinimalMode()
    {
        await using var fullClient = await ConnectClient(new ClientCapabilities
        {
            Sampling = new SamplingCapability(),
            Roots = new RootsCapability(),
            Elicitation = new ElicitationCapability(),
        });

        await using var minimalClient = await ConnectClient(new ClientCapabilities());

        var fullTools = (await fullClient.ListToolsAsync()).Select(t => t.Name).ToList();
        var minimalTools = (await minimalClient.ListToolsAsync()).Select(t => t.Name).ToList();

        await Assert.That(fullTools.Count).IsGreaterThan(minimalTools.Count);
        await Assert.That(fullTools).Contains("ai_summarize");
        await Assert.That(minimalTools).DoesNotContain("ai_summarize");
    }

    [Test]
    public async Task ServerExposesHttpEndpointAndAcceptsConnections()
    {
        await Assert.That(_serverUrl).IsNotNull();
        await Assert.That(_serverUrl).StartsWith("http://");

        await using var client = await ConnectClient(new ClientCapabilities());

        await Assert.That(client).IsNotNull();
    }
}
