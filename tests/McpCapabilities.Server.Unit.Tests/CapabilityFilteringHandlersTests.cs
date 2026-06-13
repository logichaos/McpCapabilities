using System.Text.Json.Nodes;
using McpCapabilities.Server;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpCapabilities.Server.Unit.Tests;

public class CapabilityFilteringHandlersTests
{
    private static Tool CreateTool(string name, CapabilityFlag? required = null)
    {
        var tool = new Tool { Name = name };
        if (required.HasValue && required.Value != CapabilityFlag.None)
        {
            var reqs = new ClientCapabilityRequirements { Required = required.Value };
            tool.Meta ??= [];
            reqs.WriteToMeta(tool.Meta);
        }
        return tool;
    }

    [Test]
    public async Task WrapListTools_FiltersUnsatisfiedTools()
    {
        var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
            (request, ct) =>
            {
                var result = new ListToolsResult
                {
                    Tools =
                    [
                        CreateTool("sampling_tool", CapabilityFlag.Sampling),
                        CreateTool("no_reqs_tool"),
                    ],
                };
                return ValueTask.FromResult(result);
            });

        var wrapped = CapabilityFilteringHandlers.WrapListTools(innerHandler, _ => CapabilityFlag.None);
        var result = await wrapped(default!, default);

        await Assert.That(result.Tools).Count().IsEqualTo(1);
        await Assert.That(result.Tools[0].Name).IsEqualTo("no_reqs_tool");
    }

    [Test]
    public async Task WrapListTools_PreservesSatisfiedTools()
    {
        var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
            (request, ct) =>
            {
                var result = new ListToolsResult
                {
                    Tools =
                    [
                        CreateTool("sampling_tool", CapabilityFlag.Sampling),
                        CreateTool("roots_tool", CapabilityFlag.Roots),
                    ],
                };
                return ValueTask.FromResult(result);
            });

        var wrapped = CapabilityFilteringHandlers.WrapListTools(innerHandler, _ => CapabilityFlag.Sampling);
        var result = await wrapped(default!, default);

        await Assert.That(result.Tools).Count().IsEqualTo(1);
        await Assert.That(result.Tools[0].Name).IsEqualTo("sampling_tool");
    }

    [Test]
    public async Task WrapListTools_UnannotatedToolsAlwaysVisible()
    {
        var innerHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(
            (request, ct) =>
            {
                var result = new ListToolsResult
                {
                    Tools =
                    [
                        CreateTool("tool1"),
                        CreateTool("tool2"),
                    ],
                };
                return ValueTask.FromResult(result);
            });

        var wrapped = CapabilityFilteringHandlers.WrapListTools(innerHandler, _ => CapabilityFlag.None);
        var result = await wrapped(default!, default);

        await Assert.That(result.Tools).Count().IsEqualTo(2);
    }

    [Test]
    public async Task WrapListTools_NullInnerHandler_ReturnsEmptyList()
    {
        var wrapped = CapabilityFilteringHandlers.WrapListTools(null, _ => CapabilityFlag.Sampling);
        var result = await wrapped(default!, default);

        await Assert.That(result.Tools).IsNotNull();
        await Assert.That(result.Tools).Count().IsEqualTo(0);
    }

    [Test]
    public async Task WrapListPrompts_FiltersUnsatisfiedPrompts()
    {
        var samplingPrompt = new Prompt { Name = "sampling_prompt" };
        var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Sampling };
        samplingPrompt.Meta ??= [];
        reqs.WriteToMeta(samplingPrompt.Meta);

        var nonePrompt = new Prompt { Name = "no_reqs_prompt" };

        var innerHandler = new McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>(
            (request, ct) =>
            {
                var result = new ListPromptsResult
                {
                    Prompts = [samplingPrompt, nonePrompt],
                };
                return ValueTask.FromResult(result);
            });

        var wrapped = CapabilityFilteringHandlers.WrapListPrompts(innerHandler, _ => CapabilityFlag.None);
        var result = await wrapped(default!, default);

        await Assert.That(result.Prompts).Count().IsEqualTo(1);
        await Assert.That(result.Prompts[0].Name).IsEqualTo("no_reqs_prompt");
    }

    [Test]
    public async Task WrapListResources_FiltersUnsatisfiedResources()
    {
        var rootsResource = new Resource { Name = "roots_resource", Uri = "resource://roots" };
        var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Roots };
        rootsResource.Meta ??= [];
        reqs.WriteToMeta(rootsResource.Meta);

        var noneResource = new Resource { Name = "no_reqs_resource", Uri = "resource://none" };

        var innerHandler = new McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>(
            (request, ct) =>
            {
                var result = new ListResourcesResult
                {
                    Resources = [rootsResource, noneResource],
                };
                return ValueTask.FromResult(result);
            });

        var wrapped = CapabilityFilteringHandlers.WrapListResources(innerHandler, _ => CapabilityFlag.None);
        var result = await wrapped(default!, default);

        await Assert.That(result.Resources).Count().IsEqualTo(1);
        await Assert.That(result.Resources[0].Name).IsEqualTo("no_reqs_resource");
    }
}
