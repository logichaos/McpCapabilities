using ModelContextProtocol.Protocol;

namespace BlazorClient.Infrastructure;

internal class SamplingPopupService
{
  private TaskCompletionSource<CreateMessageResult>? _tcs;

  public event Action? OnChanged;

  public bool IsVisible => _tcs is not null;

  public CreateMessageRequestParams? CurrentRequest { get; private set; }

  public Task<CreateMessageResult> WaitForUserResponseAsync(CreateMessageRequestParams request)
  {
    _tcs?.TrySetCanceled();
    _tcs = new TaskCompletionSource<CreateMessageResult>();
    CurrentRequest = request;
    OnChanged?.Invoke();
    return _tcs.Task;
  }

  public void Submit(string userText)
  {
    if (_tcs is null) return;

    var result = new CreateMessageResult
    {
      Model = "user-input",
      Role = Role.Assistant,
      Content = [new TextContentBlock { Text = userText }],
      StopReason = "endTurn",
    };

    CurrentRequest = null;
    _tcs.SetResult(result);
    _tcs = null;
    OnChanged?.Invoke();
  }

  public void Cancel()
  {
    if (_tcs is null) return;

    var result = new CreateMessageResult
    {
      Model = "user-cancelled",
      Role = Role.Assistant,
      Content = [new TextContentBlock { Text = "(cancelled by user)" }],
      StopReason = "endTurn",
    };

    CurrentRequest = null;
    _tcs.SetResult(result);
    _tcs = null;
    OnChanged?.Invoke();
  }
}