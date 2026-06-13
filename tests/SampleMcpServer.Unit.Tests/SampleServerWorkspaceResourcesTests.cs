using System.Reflection;
using McpCapabilities.Server;
using ModelContextProtocol.Server;
using SampleMcpServer;

namespace SampleMcpServer.Unit.Tests;

public class SampleServerWorkspaceResourcesTests
{
    [Test]
    public async Task Class_HasMcpServerResourceTypeAttribute()
    {
        var attr = typeof(WorkspaceResources).GetCustomAttribute<McpServerResourceTypeAttribute>();

        await Assert.That(attr).IsNotNull();
    }

    [Test]
    public async Task WorkspaceFiles_HasMcpServerResourceAttribute()
    {
        var method = typeof(WorkspaceResources).GetMethod(nameof(WorkspaceResources.WorkspaceFiles));
        var attr = method!.GetCustomAttribute<McpServerResourceAttribute>();

        await Assert.That(attr).IsNotNull();
    }

    [Test]
    public async Task WorkspaceFiles_HasRequiredClientCapabilities_Roots()
    {
        var method = typeof(WorkspaceResources).GetMethod(nameof(WorkspaceResources.WorkspaceFiles));
        var attr = method!.GetCustomAttribute<RequiredClientCapabilitiesAttribute>();

        await Assert.That(attr).IsNotNull();
        await Assert.That(attr!.Required).IsEqualTo(CapabilityFlag.Roots);
        await Assert.That(attr.Message).IsEqualTo("Requires filesystem root listing support");
    }

    [Test]
    public async Task AppInfo_HasMcpServerResourceAttribute()
    {
        var method = typeof(WorkspaceResources).GetMethod(nameof(WorkspaceResources.AppInfo));
        var attr = method!.GetCustomAttribute<McpServerResourceAttribute>();

        await Assert.That(attr).IsNotNull();
    }

    [Test]
    public async Task AppInfo_DoesNotHaveRequiredClientCapabilities()
    {
        var method = typeof(WorkspaceResources).GetMethod(nameof(WorkspaceResources.AppInfo));
        var attr = method!.GetCustomAttribute<RequiredClientCapabilitiesAttribute>();

        await Assert.That(attr).IsNull();
    }

    [Test]
    public async Task WorkspaceFiles_ReturnsNonEmptyString()
    {
        var resources = new WorkspaceResources();

        var result = resources.WorkspaceFiles();

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEmpty();
    }

    [Test]
    public async Task AppInfo_ReturnsNonEmptyString()
    {
        var resources = new WorkspaceResources();

        var result = resources.AppInfo();

        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEmpty();
    }

    [Test]
    public async Task WorkspaceFiles_Method_ReturnsStringTakesNoArgs()
    {
        var method = typeof(WorkspaceResources).GetMethod(nameof(WorkspaceResources.WorkspaceFiles))!;

        await Assert.That(method.ReturnType).IsEqualTo(typeof(string));
        await Assert.That(method.GetParameters()).IsEmpty();
    }

    [Test]
    public async Task AppInfo_Method_ReturnsStringTakesNoArgs()
    {
        var method = typeof(WorkspaceResources).GetMethod(nameof(WorkspaceResources.AppInfo))!;

        await Assert.That(method.ReturnType).IsEqualTo(typeof(string));
        await Assert.That(method.GetParameters()).IsEmpty();
    }
}
