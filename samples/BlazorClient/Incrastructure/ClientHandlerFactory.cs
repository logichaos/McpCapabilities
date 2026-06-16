using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace BlazorClient.Infrastructure;

internal static class ClientHandlerFactory
{
  public static McpClientHandlers Create(IServiceProvider services)
  {
    return new McpClientHandlers
    {
      SamplingHandler = CreateSamplingHandler(services),
      ElicitationHandler = CreateElicitationHandler(services),
    };
  }

  private static Func<CreateMessageRequestParams?, IProgress<ProgressNotificationValue>?, CancellationToken, ValueTask<CreateMessageResult>> CreateSamplingHandler(IServiceProvider services)
  {
    return async (request, progress, cancellationToken) =>
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
    };
  }

  private static Func<ElicitRequestParams?, CancellationToken, ValueTask<ElicitResult>> CreateElicitationHandler(IServiceProvider services)
  {
    return async (request, cancellationToken) =>
    {
      var popup = services.GetRequiredService<ElicitationPopupService>();

      if (request is null)
      {
        return new ElicitResult { Action = "decline" };
      }

      return await popup.WaitForUserResponseAsync(request);
    };
  }
}