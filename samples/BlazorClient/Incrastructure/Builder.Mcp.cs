using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
namespace BlazorClient.Infrastructure;

internal static partial class Builder
{
  public static IServiceCollection AddMcp(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddKeyedScoped("httpOptions", (services, obj) =>
    {
      HttpClientTransportOptions options = new()
      {
        Endpoint = new Uri(configuration.GetValue<string>("Mcp:ServerEndpoint") ?? "https://localhost:5000"),
      };

      return options;
    });


    services.AddKeyedScoped<IClientTransport>("httpTransport", (services, obj) =>
    {
      var options = services.GetRequiredKeyedService<HttpClientTransportOptions>("httpOptions");
      HttpClientTransport transport = new(options);

      return transport;
    });

    services.AddKeyedScoped("fullClientCaps", (services, _) =>
    {
      return new ClientCapabilities
      {
        Sampling = new SamplingCapability(),
        Roots = new RootsCapability(),
        Elicitation = new ElicitationCapability(),
      };
    });
    services.AddKeyedScoped("mcpClientOptions", (services, obj) =>
    {
      return new McpClientOptions()
      {
        Capabilities = services.GetRequiredKeyedService<ClientCapabilities>("fullClientCaps"),
        ClientInfo = new Implementation { Name = "BlazorClient", Version = "0.1.0" },
      };
    });
    
    return services;
  }
}