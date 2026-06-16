using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace BlazorClient.Infrastructure;

internal static class SamplingHandlerFactory
{
  public static McpClientHandlers Create(IServiceProvider services)
  {
    return new McpClientHandlers
    {
      SamplingHandler = async (request, progress, cancellationToken) =>
      {
        var popup = services.GetRequiredService<SamplingPopupService>();

        if (request is null)
        {
          return new CreateMessageResult
          {
            Model = "error",
            Role = Role.Assistant,
            Content = [new TextContentBlock { Text = "(no sampling request provided)" }],
          };
        }

        return await popup.WaitForUserResponseAsync(request);
      }
    };
  }
}
