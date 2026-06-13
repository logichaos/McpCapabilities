using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var serverUrl = args.Length > 0 ? args[0] : "https://localhost:5001";

Console.WriteLine("=== MCP HTTP Sample Client ===");
Console.WriteLine($"Server URL: {serverUrl}");
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

    var transport = new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri(serverUrl),
    });

    var options = new McpClientOptions
    {
        ClientInfo = new Implementation { Name = "SampleHttpClient", Version = "1.0" },
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
