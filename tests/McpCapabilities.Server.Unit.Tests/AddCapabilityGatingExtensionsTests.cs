using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class AddCapabilityGatingExtensionsTests
{
    [Test]
    public async Task AddCapabilityGating_ReturnsBuilderForChaining()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<McpServerOptions>(opt => opt.Handlers = new McpServerHandlers());
        var builderMock = new TestMcpServerBuilder(services);

        var result = builderMock.AddCapabilityGating();

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task AddCapabilityGating_ConfiguresHandlers()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<McpServerOptions>(opt => opt.Handlers = new McpServerHandlers());
        var builderMock = new TestMcpServerBuilder(services);

        builderMock.AddCapabilityGating();

        var sp = services.BuildServiceProvider();
        var resolvedOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var listTools = resolvedOptions.Handlers.ListToolsHandler;
        var listPrompts = resolvedOptions.Handlers.ListPromptsHandler;
        var listResources = resolvedOptions.Handlers.ListResourcesHandler;
        await Assert.That(listTools is not null).IsTrue();
        await Assert.That(listPrompts is not null).IsTrue();
        await Assert.That(listResources is not null).IsTrue();
    }

    [Test]
    public async Task AddCapabilityGating_WrapsExistingHandler()
    {
        var services = new ServiceCollection();
        services.AddOptions();

        var existingHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
            (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [] }));

        services.Configure<McpServerOptions>(opt =>
        {
            opt.Handlers = new McpServerHandlers
            {
                ListToolsHandler = existingHandler,
            };
        });
        var builderMock = new TestMcpServerBuilder(services);

        builderMock.AddCapabilityGating();

        var sp = services.BuildServiceProvider();
        var resolvedOptions = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;

        var wrappedHandler = resolvedOptions.Handlers.ListToolsHandler;
        await Assert.That(wrappedHandler is not null).IsTrue();
        await Assert.That(!ReferenceEquals(wrappedHandler, existingHandler)).IsTrue();
    }
}

internal sealed class TestMcpServerBuilder : IMcpServerBuilder
{
    public IServiceCollection Services { get; }

    public TestMcpServerBuilder(IServiceCollection services)
    {
        Services = services;
    }
}
