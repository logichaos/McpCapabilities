using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace BlazorClient.Infrastructure;

internal class ElicitationPopupService
{
  private TaskCompletionSource<ElicitResult>? _tcs;

  public event Action? OnChanged;

  public bool IsVisible => _tcs is not null;

  public ElicitRequestParams? CurrentRequest { get; private set; }

  public Task<ElicitResult> WaitForUserResponseAsync(ElicitRequestParams request)
  {
    _tcs?.TrySetCanceled();
    _tcs = new TaskCompletionSource<ElicitResult>();
    CurrentRequest = request;
    OnChanged?.Invoke();
    return _tcs.Task;
  }

  public void Submit(Dictionary<string, object> fieldValues)
  {
    if (_tcs is null) return;

    var content = new Dictionary<string, JsonElement>();
    foreach (var (key, value) in fieldValues)
    {
      content[key] = JsonSerializer.SerializeToElement(value);
    }

    var result = new ElicitResult
    {
      Action = "accept",
      Content = content,
    };

    CurrentRequest = null;
    _tcs.SetResult(result);
    _tcs = null;
    OnChanged?.Invoke();
  }

  public void Decline()
  {
    if (_tcs is null) return;

    var result = new ElicitResult { Action = "decline" };

    CurrentRequest = null;
    _tcs.SetResult(result);
    _tcs = null;
    OnChanged?.Invoke();
  }
}
