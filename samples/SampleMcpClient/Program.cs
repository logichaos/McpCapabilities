using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var serverProjectDir = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
        "samples", "SampleMcpServer"));

Console.WriteLine("=== MCP Sample Client ===");
Console.WriteLine();

await RunClientProfile(
    "FULL (Sampling + Roots + Elicitation)",
    new ClientCapabilities
    {
        Sampling = new SamplingCapability(),
        Roots = new RootsCapability(),
        Elicitation = new ElicitationCapability(),
    });

await RunClientProfile(
    "MINIMAL (no capabilities)",
    new ClientCapabilities());

Console.WriteLine("Done.");

async Task RunClientProfile(string label, ClientCapabilities capabilities)
{
    Console.WriteLine($"--- Profile: {label} ---");

    var transport = new StdioClientTransport(new StdioClientTransportOptions
    {
        Command = "dotnet",
        Arguments = ["run", "--project", serverProjectDir, "--no-build"],
    });

    var options = new McpClientOptions
    {
        ClientInfo = new Implementation { Name = "SampleClient", Version = "1.0" },
        Capabilities = capabilities,
    };

    await using var client = await McpClient.CreateAsync(transport, options);

    var tools = await client.ListToolsAsync();
    Console.WriteLine($"  Tools ({tools.Count}):");
    foreach (var t in tools)
        Console.WriteLine($"    - {t.Name}");

    var prompts = await client.ListPromptsAsync();
    Console.WriteLine($"  Prompts ({prompts.Count}):");
    foreach (var p in prompts)
        Console.WriteLine($"    - {p.Name}");

    var resources = await client.ListResourcesAsync();
    Console.WriteLine($"  Resources ({resources.Count}):");
    foreach (var r in resources)
        Console.WriteLine($"    - {r.Name}");

    Console.WriteLine();
}
