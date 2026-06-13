# Monadic / Union-Type Approach to Capability Gating — Design Analysis

This document explores replacing exception-based capability gating with **monadic result
types** and **discriminated unions**. It compares three approaches, shows concrete API
designs for both the Server and Client libraries, and discusses trade-offs.

---

## 1. The Three Approaches Compared

| | Exception-Based | Result Monad (Custom) | OneOf Union | FluentResults |
|---|---|---|---|---|
| **Flow control** | `throw` / `try-catch` | Return `Result<T,E>` | Return `OneOf<T, E1, E2>` | Return `Result<T>` with `.Reasons` |
| **Composability** | Poor — breaks linear flow | Good — `Map`, `Bind`, `Match` | Good — `.Match()`, `.Switch()` | Excellent — `Bind`, `Map`, `Check`, `Merge` |
| **Exhaustiveness** | Compiler can't help | Roll your own `Match` | `.Match()` forces handling all cases | `.HasError()` checks; no exhaustion enforcement |
| **Async support** | Natural (`try-catch` works) | Needs `AsyncResult` or `Task<Result<>>` | Same as Result — `Task<OneOf<>>` | First-class via `Task<Result<T>>` + `BindAsync` |
| **Allocation** | Exception objects (costly) | Value-type Result (cheap) | Value-type union (cheap) | Reference-type (heap alloc per result) |
| **Stack traces** | Preserved | Lost (by design) | Lost (by design) | Optional via `ExceptionalError` |
| **Library dependency** | None | None | `OneOf` NuGet package | `FluentResults` NuGet package |
| **C# ergonomics** | Native | Pattern matching (C# 8+) | `.Match()` / `.Switch()` | Fluent `.WithError().WithSuccess()` |
| **Multiple errors** | AggregateException | Single error | N/A | First-class (`.Reasons` list) |
| **Logging integration** | Manual | Manual | Manual | Built-in `.Log()` / `ILogger` |
| **HTTP/API integration** | Manual `ProblemDetails` | Manual | Manual | `.ToResultDto()`, `.ToActionResult()` |
| **Debugging** | Easy — debugger breaks on throw | Harder — need to inspect return | Harder — need to inspect return |
| **Learning curve** | None | Moderate | Low (OneOf is well-known) |

---

## 2. Approach A — Custom Result Monad (No Dependencies)

### 2.1 The Core Types

```csharp
// ── Error types (value types — zero allocation on success path) ──

/// <summary>
/// Describes why a capability requirement was not met.
/// </summary>
public readonly record struct CapabilityError
{
    public CapabilityFlag Required { get; init; }
    public CapabilityFlag Missing { get; init; }
    public string PrimitiveName { get; init; }
    public string? Message { get; init; }

    public override string ToString() =>
        $"'{PrimitiveName}' requires {Required}. Server/client is missing: {Missing}. " +
        (Message ?? "");
}

/// <summary>
/// Discriminated union: either a value of T or a CapabilityError.
/// All methods are zero-allocation on the happy path (value-type union).
/// </summary>
public readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly CapabilityError _error;
    private readonly bool _isSuccess;

    private Result(T value)
    {
        _value = value;
        _error = default;
        _isSuccess = true;
    }

    private Result(CapabilityError error)
    {
        _value = default;
        _error = error;
        _isSuccess = false;
    }

    public bool IsSuccess => _isSuccess;
    public bool IsError => !_isSuccess;

    public T Value => _isSuccess ? _value! :
        throw new InvalidOperationException($"Result is an error: {_error}");

    public CapabilityError Error => !_isSuccess ? _error :
        throw new InvalidOperationException("Result is a success");

    // ── Smart constructors ──
    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(CapabilityError error) => new(error);

    // ── Monadic combinators ──

    /// <summary>Transform the value if success.</summary>
    public Result<U> Map<U>(Func<T, U> mapper) =>
        _isSuccess ? Result<U>.Ok(mapper(_value!)) : Result<U>.Fail(_error);

    /// <summary>Chain another operation that may fail.</summary>
    public Result<U> Bind<U>(Func<T, Result<U>> binder) =>
        _isSuccess ? binder(_value!) : Result<U>.Fail(_error);

    /// <summary>Pattern-match both cases.</summary>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<CapabilityError, TResult> onError) =>
        _isSuccess ? onSuccess(_value!) : onError(_error);

    /// <summary>Execute side effects for each case.</summary>
    public Result<T> Tap(
        Action<T>? onSuccess = null,
        Action<CapabilityError>? onError = null)
    {
        if (_isSuccess) onSuccess?.Invoke(_value!);
        else onError?.Invoke(_error);
        return this;
    }

    // ── Implicit conversion for ergonomics ──
    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(CapabilityError error) => Fail(error);
}
```

### 2.2 Async Result Extensions

```csharp
/// <summary>
/// Extension methods for working with Task<Result<T>> in async flows.
/// </summary>
public static class AsyncResultExtensions
{
    /// <summary>Map over an async result.</summary>
    public static async Task<Result<U>> MapAsync<T, U>(
        this Task<Result<T>> task,
        Func<T, U> mapper)
    {
        var result = await task;
        return result.Map(mapper);
    }

    /// <summary>Bind over an async result.</summary>
    public static async Task<Result<U>> BindAsync<T, U>(
        this Task<Result<T>> task,
        Func<T, Task<Result<U>>> binder)
    {
        var result = await task;
        return result.IsSuccess
            ? await binder(result.Value)
            : Result<U>.Fail(result.Error);
    }

    /// <summary>Convert a value to an async result.</summary>
    public static Task<Result<T>> AsAsyncResult<T>(this T value)
        => Task.FromResult(Result<T>.Ok(value));

    /// <summary>Convert an error to an async result.</summary>
    public static Task<Result<T>> AsAsyncResultError<T>(this CapabilityError error)
        => Task.FromResult(Result<T>.Fail(error));
}
```

### 2.3 Server-Side — Monadic Capability Check

Instead of silently filtering, return a list annotated with why each item was excluded:

```csharp
// ── Enriched result type for the server ──

public readonly record struct FilteredPrimitive<T>
{
    public T Primitive { get; init; }
    public bool IsVisible { get; init; }
    public CapabilityFlag? MissingFlags { get; init; }
    public string? ExclusionReason { get; init; }
}

public readonly record struct FilteredListResult<T>
{
    public IReadOnlyList<T> Visible { get; init; }
    public IReadOnlyList<FilteredPrimitive<T>> Hidden { get; init; }
    public int TotalCount => Visible.Count + Hidden.Count;

    /// <summary>
    /// Returns the visible items AND optionally logs/notifies about hidden ones.
    /// The ListToolsResult itself only contains visible tools.
    /// </summary>
    public ListToolsResult ToListToolsResult(
        Action<IReadOnlyList<FilteredPrimitive<T>>>? onHidden = null)
    {
        onHidden?.Invoke(Hidden);
        return new ListToolsResult { Tools = Visible.Cast<Tool>().ToList() };
    }
}
```

```csharp
// ── The filtering logic — now returns a Result, not a side-effect ──

public static Result<FilteredListResult<Tool>> FilterToolsByCapabilities(
    List<Tool> tools,
    ClientCapabilities? clientCaps)
{
    var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
    var visible = new List<Tool>();
    var hidden = new List<FilteredPrimitive<Tool>>();

    foreach (var tool in tools)
    {
        var reqs = ClientCapabilityRequirements.ReadFromMeta(tool.Meta);

        if (reqs.Required == CapabilityFlag.None)
        {
            visible.Add(tool);
        }
        else if ((clientFlags & reqs.Required) == reqs.Required)
        {
            visible.Add(tool);
        }
        else
        {
            var missing = reqs.Required & ~clientFlags;
            hidden.Add(new FilteredPrimitive<Tool>
            {
                Primitive = tool,
                IsVisible = false,
                MissingFlags = missing,
                ExclusionReason = reqs.Message
            });
        }
    }

    // If all tools were filtered out, the whole result could be considered degraded
    if (visible.Count == 0 && hidden.Count > 0)
    {
        return Result<FilteredListResult<Tool>>.Fail(new CapabilityError
        {
            Required = hidden
                .Aggregate(CapabilityFlag.None, (acc, h) => acc | (h.MissingFlags ?? CapabilityFlag.None)),
            Missing = hidden
                .Aggregate(CapabilityFlag.None, (acc, h) => acc | (h.MissingFlags ?? CapabilityFlag.None)),
            PrimitiveName = "tools/list",
            Message = $"All {hidden.Count} tools require capabilities the client lacks"
        });
    }

    return new FilteredListResult<Tool> { Visible = visible, Hidden = hidden };
}
```

### 2.4 Client-Side — Monadic Tool Invocation

```csharp
// ── Capability check that returns Result instead of throwing ──

public static Result<Unit> CheckCapabilities(
    ServerCapabilityRequirements requirements,
    ServerCapabilities? serverCaps,
    string operationName)
{
    if (requirements.Required == CapabilityFlag.None)
        return Unit.Value;

    var available = CapabilityFlags.FromServerCapabilities(serverCaps);
    if ((available & requirements.Required) == requirements.Required)
        return Unit.Value;

    var missing = requirements.Required & ~available;
    return new CapabilityError
    {
        Required = requirements.Required,
        Missing = missing,
        PrimitiveName = operationName,
        Message = requirements.Message
    };
}

// Utility: represents a void success (like F#'s unit)
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
```

```csharp
// ── The CapabilityAwareMcpClient with monadic API ──

public class MonadicCapabilityAwareMcpClient : IAsyncDisposable
{
    private readonly McpClient _inner;

    public MonadicCapabilityAwareMcpClient(McpClient inner)
    {
        _inner = inner;
    }

    public ServerCapabilities ServerCapabilities => _inner.ServerCapabilities;

    // ── Monadic: returns Result<CallToolResult> ──

    public async Task<Result<CallToolResult>> TryCallToolAsync(
        string toolName,
        ServerCapabilityRequirements requirements,
        IReadOnlyDictionary<string, object?>? arguments = null,
        CancellationToken ct = default)
    {
        // 1. Check capabilities — pure, no exception
        var check = CheckCapabilities(requirements, ServerCapabilities, toolName);
        if (check.IsError)
            return check.Error;  // implicit conversion to Result<CallToolResult>.Fail(...)

        // 2. Only proceed if capabilities are met
        var result = await _inner.CallToolAsync(toolName, arguments, cancellationToken: ct);
        return result;  // implicit conversion to Result<CallToolResult>.Ok(...)
    }

    // ── Fluent chaining example ──

    public async Task<Result<string>> TryGetAlertsAndSummarize(
        string state,
        CancellationToken ct = default)
    {
        // Check tools capability
        var toolsCheck = CheckCapabilities(
            new ServerCapabilityRequirements { Required = CapabilityFlag.Tools },
            ServerCapabilities, "tools/list");

        if (toolsCheck.IsError)
            return toolsCheck.Error;

        // Get alerts
        var result = await _inner.CallToolAsync("get_alerts",
            new Dictionary<string, object?> { ["state"] = state },
            cancellationToken: ct);

        return ((TextContentBlock)result.Content[0]).Text;
    }

    // ── Pattern matching in the caller ──

    public async ValueTask DisposeAsync() => await _inner.DisposeAsync();
}
```

### 2.5 Client Caller — How the UX Changes

```csharp
// ── Exception-based (current) ──
try
{
    var result = await client.CallToolAsync("ai_summarize", reqs, args);
    Console.WriteLine(result.Content[0].Text);
}
catch (CapabilityNotAvailableException ex)
{
    Console.WriteLine($"Tool not available: {ex.Missing}");
}
catch (McpException ex)
{
    Console.WriteLine($"Protocol error: {ex.Message}");
}

// ── Monadic (Result<T>) ──
var result = await client.TryCallToolAsync("ai_summarize", reqs, args);

result.Match(
    onSuccess: r => Console.WriteLine(r.Content[0].Text),
    onError: e =>
    {
        // e.Missing tells us exactly which flags are missing
        // e.Required tells us what was needed
        // No stack trace overhead, no exception allocation
        if (e.Missing.HasFlag(CapabilityFlag.Sampling))
            Console.WriteLine("Server doesn't support AI sampling");
        else
            Console.WriteLine($"Capability error: {e}");
    });

// ── Monadic chaining (more typical usage) ──
var finalText = await client
    .TryCallToolAsync("get_alerts",
        new ServerCapabilityRequirements { Required = CapabilityFlag.Tools },
        new() { ["state"] = "WA" })
    .BindAsync(async alerts =>
    {
        // alerts is a CallToolResult — we're inside the happy path
        var text = ((TextContentBlock)alerts.Content[0]).Text;

        // Now try to summarize (may fail if server doesn't support prompts)
        return await client.TryGetPromptAsync("summarize",
            new ServerCapabilityRequirements { Required = CapabilityFlag.Prompts },
            new() { ["text"] = text });
    })
    .MapAsync(promptResult =>
    {
        // Both operations succeeded — transform the final result
        return promptResult.Messages[0].Content.Text ?? "";
    });

// Handle errors at the outermost level
finalText.Match(
    onSuccess: text => Console.WriteLine($"Summary: {text}"),
    onError: err => Console.WriteLine($"Failed: {err}")
);

// ── Or: fail fast, convert to exception at the boundary ──
if (finalText.IsError)
    throw new CapabilityNotAvailableException(finalText.Error.Missing, finalText.Error.ToString());
var text2 = finalText.Value; // safe — we already checked
```

---

## 3. Approach B — OneOf Library (Popular NuGet Package)

[OneOf](https://github.com/mcintyre321/OneOf) is the most popular discriminated union
library for C# (200M+ downloads). It provides `OneOf<T0, T1, T2>` types up to 9
generics with exhaustive `.Match()` and `.Switch()`.

### 3.1 Package Reference

```xml
<PackageReference Include="OneOf" Version="3.*" />
```

### 3.2 Union Types for Capability Results

```csharp
using OneOf;

// ── Define what a tool call can return ──

// Success case — normal result
// First error — capability not met
// Second error — MCP protocol error
// Third error — network/transport error
using CallToolResultOrError = OneOf<
    CallToolResult,
    CapabilityError,
    McpError,
    TransportError>;

// Define meaningfully-named error types
public readonly record struct McpError(string Message, McpErrorCode Code);
public readonly record struct TransportError(string Message, Exception? Inner);

// ── Or simpler: only distinguish capability errors ──
using CallToolWithCapabilityResult = OneOf<CallToolResult, CapabilityError>;
```

### 3.3 Client with OneOf

```csharp
public class OneOfCapabilityAwareMcpClient : IAsyncDisposable
{
    private readonly McpClient _inner;

    public OneOfCapabilityAwareMcpClient(McpClient inner) { _inner = inner; }

    public async Task<CallToolWithCapabilityResult> TryCallToolAsync(
        string toolName,
        ServerCapabilityRequirements requirements,
        IReadOnlyDictionary<string, object?>? arguments = null,
        CancellationToken ct = default)
    {
        // Check capabilities
        var available = CapabilityFlags.FromServerCapabilities(ServerCapabilities);
        if ((available & requirements.Required) != requirements.Required)
        {
            var missing = requirements.Required & ~available;
            return new CapabilityError
            {
                Required = requirements.Required,
                Missing = missing,
                PrimitiveName = toolName,
                Message = requirements.Message
            };
        }

        // Call the tool — wrap MCP exceptions too
        try
        {
            return await _inner.CallToolAsync(
                toolName, arguments, cancellationToken: ct);
        }
        catch (McpException ex)
        {
            return new CapabilityError
            {
                PrimitiveName = toolName,
                Message = $"MCP error: {ex.Message} (code: {ex.ErrorCode})"
            };
        }
    }

    public async ValueTask DisposeAsync() => await _inner.DisposeAsync();
}
```

### 3.4 Caller Experience with OneOf

```csharp
var result = await client.TryCallToolAsync("ai_summarize", reqs, args);

// ── Method 1: Match with lambdas (exhaustive — compiler ensures all cases handled) ──
string output = result.Match(
    success: callResult => ((TextContentBlock)callResult.Content[0]).Text,
    error: capError => $"Error: {capError.Missing} not available"
);

// ── Method 2: Switch (side effects) ──
result.Switch(
    success: r => Console.WriteLine(r.Content[0].Text),
    error: e => Console.Error.WriteLine($"Missing: {e.Missing}")
);

// ── Method 3: Pattern matching with is/as (familiar C# idiom) ──
if (result.TryPickT0(out var callResult, out var remaining))
{
    Console.WriteLine($"Success: {callResult.Content[0]}");
}
else
{
    var error = remaining.AsT0; // CapabilityError
    Console.WriteLine($"Error: {error.Missing}");
}

// ── Method 4: Linq-style chaining (OneOf supports Select/SelectMany) ──
var chained = from r1 in await client.TryCallToolAsync("get_alerts", reqs1, args1)
              from r2 in await client.TryCallToolAsync("ai_summarize", reqs2, args2)
              select $"{r1.Content[0]} + {r2.Content[0]}";
// If either fails, chained is the error
```

### 3.5 Server-Side with OneOf — Filtered List

```csharp
// Return type: either an enriched list or a "nothing visible" error
using FilteredToolsResult = OneOf<FilteredListResult<Tool>, CapabilityError>;

public static FilteredToolsResult FilterTools(
    IReadOnlyList<Tool> tools,
    ClientCapabilities? clientCaps)
{
    var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
    var visible = new List<Tool>();
    var hidden = new List<FilteredPrimitive<Tool>>();

    foreach (var tool in tools)
    {
        var reqs = ClientCapabilityRequirements.ReadFromMeta(tool.Meta);
        if (reqs.Required == CapabilityFlag.None || (clientFlags & reqs.Required) == reqs.Required)
            visible.Add(tool);
        else
            hidden.Add(new FilteredPrimitive<Tool>
            {
                Primitive = tool,
                MissingFlags = reqs.Required & ~clientFlags,
                ExclusionReason = reqs.Message
            });
    }

    // Choose: return visible-only (even if empty) or error if all hidden
    return new FilteredListResult<Tool> { Visible = visible, Hidden = hidden };
}
```

---

## 4. Approach D — FluentResults (Rich Error Model)

[FluentResults](https://github.com/altmann/FluentResults) (100M+ downloads) is the
most mature functional-result library for .NET. Unlike the simpler approaches above,
it provides a **rich error model** with multiple reasons per result, metadata, logging
hooks, and ASP.NET Core integration.

### 4.1 Package Reference

```xml
<PackageReference Include="FluentResults" Version="4.*" />
```

### 4.2 Defining Capability Error Types

FluentResults uses the `IError` interface. You define custom error classes that carry
structured data and can be pattern-matched:

```csharp
using FluentResults;

/// <summary>
/// Structured error for capability gating failures.
/// Carries enough information for callers to make programmatic decisions.
/// </summary>
public class CapabilityNotMetError : Error
{
    public CapabilityFlag Required { get; }
    public CapabilityFlag Missing { get; }
    public string PrimitiveName { get; }

    public CapabilityNotMetError(
        CapabilityFlag required,
        CapabilityFlag missing,
        string primitiveName,
        string? message = null)
        : base(message ?? $"{primitiveName} requires {required}. Missing: {missing}")
    {
        Required = required;
        Missing = missing;
        PrimitiveName = primitiveName;

        // Metadata for structured logging / diagnostics
        WithMetadata("RequiredFlags", required.ToString());
        WithMetadata("MissingFlags", missing.ToString());
        WithMetadata("PrimitiveName", primitiveName);
    }
}

/// <summary>
/// Error indicating a tool/prompt/resource was not found on the server.
/// </summary>
public class PrimitiveNotFoundError : Error
{
    public string PrimitiveName { get; }

    public PrimitiveNotFoundError(string primitiveName)
        : base($"'{primitiveName}' not found on the server")
    {
        PrimitiveName = primitiveName;
    }
}

/// <summary>
/// Error wrapping an MCP protocol-level failure.
/// </summary>
public class McpProtocolError : Error
{
    public McpErrorCode ErrorCode { get; }

    public McpProtocolError(string message, McpErrorCode code) : base(message)
    {
        ErrorCode = code;
    }
}
```

### 4.3 Client with FluentResults

```csharp
using FluentResults;

public class FluentCapabilityAwareMcpClient : IAsyncDisposable
{
    private readonly McpClient _inner;

    public FluentCapabilityAwareMcpClient(McpClient inner)
    {
        _inner = inner;
    }

    public ServerCapabilities ServerCapabilities => _inner.ServerCapabilities;

    /// <summary>
    /// Attempts to call a tool. Returns a Result that is either:
    /// - Success with CallToolResult
    /// - Failure with CapabilityNotMetError (capability missing)
    /// - Failure with PrimitiveNotFoundError (tool not on server)
    /// - Failure with McpProtocolError (MCP-level failure)
    /// </summary>
    public async Task<Result<CallToolResult>> TryCallToolAsync(
        string toolName,
        ServerCapabilityRequirements requirements,
        IReadOnlyDictionary<string, object?>? arguments = null,
        CancellationToken ct = default)
    {
        // 1. Check capabilities — build a Result from the check
        var capResult = CheckServerCapabilities(requirements, toolName);
        if (capResult.IsFailed)
            return capResult;  // propagates the CapabilityNotMetError

        // 2. Call the tool — wrap MCP exceptions as errors
        try
        {
            var toolResult = await _inner.CallToolAsync(
                toolName, arguments, cancellationToken: ct);
            return Result.Ok(toolResult);
        }
        catch (McpException ex) when (ex.ErrorCode == McpErrorCode.MethodNotFound)
        {
            return Result.Fail<CallToolResult>(
                new PrimitiveNotFoundError(toolName).CausedBy(ex));
        }
        catch (McpException ex)
        {
            return Result.Fail<CallToolResult>(
                new McpProtocolError(ex.Message, ex.ErrorCode).CausedBy(ex));
        }
    }

    /// <summary>
    /// Pure capability check — returns a Result directly.
    /// </summary>
    private Result CheckServerCapabilities(
        ServerCapabilityRequirements requirements, string operationName)
    {
        if (requirements.Required == CapabilityFlag.None)
            return Result.Ok();

        var available = CapabilityFlags.FromServerCapabilities(ServerCapabilities);
        if ((available & requirements.Required) == requirements.Required)
            return Result.Ok();

        var missing = requirements.Required & ~available;
        return Result.Fail(
            new CapabilityNotMetError(requirements.Required, missing, operationName,
                requirements.Message));
    }

    /// <summary>
    /// Returns tools filtered by capability, with reasons for exclusions.
    /// The Result.Success contains the visible tools.
    /// The Result has Successes with extra metadata about hidden tools.
    /// </summary>
    public async Task<Result<IList<McpClientTool>>> TryListFilteredToolsAsync(
        IReadOnlyDictionary<string, ServerCapabilityRequirements> requirementsByTool,
        RequestOptions? options = null,
        CancellationToken ct = default)
    {
        var tools = await _inner.ListToolsAsync(options, ct);
        var visible = new List<McpClientTool>();
        var hiddenReasons = new List<IReason>();

        foreach (var tool in tools)
        {
            if (!requirementsByTool.TryGetValue(tool.Name, out var reqs))
            {
                visible.Add(tool);
                continue;
            }

            if (reqs.Required == CapabilityFlag.None)
            {
                visible.Add(tool);
                continue;
            }

            var available = CapabilityFlags.FromServerCapabilities(ServerCapabilities);
            if ((available & reqs.Required) == reqs.Required)
            {
                visible.Add(tool);
            }
            else
            {
                // Track hidden tools as informational Successes (not Errors)
                hiddenReasons.Add(new Success(
                    $"Tool '{tool.Name}' hidden: requires {reqs.Required}, " +
                    $"server provides {available}"));
            }
        }

        var result = Result.Ok<IList<McpClientTool>>(visible);
        result.Reasons.AddRange(hiddenReasons);
        // If nothing visible AND things hidden, also add a warning
        if (visible.Count == 0 && hiddenReasons.Count > 0)
        {
            result.Reasons.Add(new Error("All tools require capabilities the server lacks"));
        }

        return result;
    }

    public async ValueTask DisposeAsync() => await _inner.DisposeAsync();
}
```

### 4.4 Caller Experience with FluentResults

```csharp
var result = await client.TryCallToolAsync("ai_summarize", reqs, args);

// ── Style 1: Imperative with HasError() ──
if (result.IsFailed)
{
    foreach (var error in result.Errors)
    {
        switch (error)
        {
            case CapabilityNotMetError c:
                Console.WriteLine($"Missing capability: {c.Missing}");
                // Optionally re-authorize with augmented scopes
                break;
            case PrimitiveNotFoundError p:
                Console.WriteLine($"Tool not found: {p.PrimitiveName}");
                break;
            case McpProtocolError m:
                Console.WriteLine($"Protocol error: {m.Message}");
                break;
        }
    }
    return;
}

var toolResult = result.Value;
Console.WriteLine(toolResult.Content[0]);

// ── Style 2: Fluent chaining with Bind ──
var summary = await client
    .TryCallToolAsync("get_alerts", reqsTools,
        new() { ["state"] = "WA" })
    .Bind(async alerts =>
    {
        var text = ((TextContentBlock)alerts.Content[0]).Text;
        return await client.TryCallToolAsync("ai_summarize", reqsSampling,
            new() { ["text"] = text });
    })
    .Bind(async summary =>
    {
        // Only reached if both calls succeeded
        var output = ((TextContentBlock)summary.Content[0]).Text;
        return Result.Ok(output);
    });

if (summary.IsSuccess)
    Console.WriteLine(summary.Value);
else
    Console.Error.WriteLine($"Pipeline failed: {summary.Errors.Count} error(s)");

// ── Style 3: With logging baked in ──
var loggedResult = await client
    .TryCallToolAsync("get_alerts", reqs, args)
    .Log(LogLevel.Information,
        onSuccess: r => $"Got alerts: {r.Content.Count} blocks",
        onError: errors => $"Failed: {string.Join("; ", errors.Select(e => e.Message))}");

// ── Style 4: Deconstruct (C# pattern matching on the result) ──
if (result.TryGetResult(out var ok, out var errorList))
{
    // Happy path
}
else
{
    // errorList has all the errors with their types
    var capabilityErrors = errorList.OfType<CapabilityNotMetError>().ToList();
}
```

### 4.5 Server-Side with FluentResults — Enriched Filtering

```csharp
using FluentResults;

public static class CapabilityFilteringFluentExtensions
{
    /// <summary>
    /// Filters tools by client capabilities, returning a Result where:
    /// - Success = the filtered tool list
    /// - Reasons = informational messages about hidden tools
    /// - Errors = if ALL tools were hidden, this is a failure
    /// </summary>
    public static Result<IList<Tool>> FilterByClientCapabilities(
        this IList<Tool> tools,
        ClientCapabilities? clientCaps)
    {
        var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
        var visible = new List<Tool>();
        var hiddenCount = 0;

        var result = new Result<IList<Tool>>();

        foreach (var tool in tools)
        {
            var reqs = ClientCapabilityRequirements.ReadFromMeta(tool.Meta);

            if (reqs.Required == CapabilityFlag.None)
            {
                visible.Add(tool);
            }
            else if ((clientFlags & reqs.Required) == reqs.Required)
            {
                visible.Add(tool);
            }
            else
            {
                hiddenCount++;
                var missing = reqs.Required & ~clientFlags;
                // Add as informational reason, not error
                result.WithReason(new Success(
                    $"Skipping '{tool.Name}': requires {reqs.Required}, client has {clientFlags}. " +
                    $"Missing: {missing}. {reqs.Message ?? ""}"));
            }
        }

        if (visible.Count == 0 && hiddenCount > 0)
        {
            return Result.Fail<IList<Tool>>(
                new CapabilityNotMetError(
                    tools.Aggregate(CapabilityFlag.None,
                        (acc, t) =>
                        {
                            var r = ClientCapabilityRequirements.ReadFromMeta(t.Meta);
                            return acc | r.Required;
                        }),
                    tools.Aggregate(CapabilityFlag.None,
                        (acc, t) =>
                        {
                            var r = ClientCapabilityRequirements.ReadFromMeta(t.Meta);
                            return acc | (r.Required & ~clientFlags);
                        }),
                    "tools/list",
                    $"None of the {tools.Count} tools are available to this client"));
        }

        return Result.Ok<IList<Tool>>(visible);
    }
}
```

### 4.6 FluentResults vs Custom Result — Trade-Off Summary

| Consideration | FluentResults | Custom `Result<T>` |
|---|---|---|
| **Multiple errors** | ✅ First-class — `result.Errors` is `List<IError>` | ❌ Single error |
| **Error hierarchy** | ✅ `IError` with 10+ built-in types | ⚠️ Roll your own discriminated union |
| **Logging** | ✅ Built-in `.Log(level, ...)` | ❌ Manual |
| **ASP.NET integration** | ✅ `.ToActionResult()` for controllers | ❌ Manual mapping |
| **Exception capture** | ✅ `.CausedBy(ex)` preserves exception chain | ⚠️ Manual `Exception` field |
| **Success reasons** | ✅ `.WithSuccess()` carries informational messages | ❌ No built-in equivalent |
| **Async** | ✅ `Task<Result<T>>` with `BindAsync` extensions | ⚠️ Write your own `BindAsync` |
| **Allocation** | ❌ Reference type — heap alloc per result + per error | ✅ Value type — zero alloc on success |
| **Dependency** | ❌ NuGet package + dependency graph | ✅ None |
| **API surface** | ⚠️ Large — 200+ methods to learn | ✅ Tiny — 5 methods |
| **Trimming / AOT** | ⚠️ May need trimming annotations | ✅ Trimmable by construction |
| **Perf on hot paths** | ❌ Allocates `Result`, `List<Error>`, `Error` objects | ✅ Stack-allocated struct |

### 4.7 When to Choose FluentResults

FluentResults shines when:

1. **Multiple errors per operation** — a single tool call might fail for capability
   reasons AND protocol reasons. FluentResults captures both.

2. **You already use it** — if your ASP.NET Core project already uses FluentResults
   for API responses (`.ToActionResult()`), adding capability gating to the same
   pipeline is natural.

3. **Rich diagnostics** — `.WithMetadata()`, `.Log()`, and `.CausedBy()` let you
   build detailed audit trails.

4. **You're wrapping exception-heavy code** — FluentResults was designed to bridge
   the exception/result gap. Its `.CausedBy()` and `ExceptionalError` make it easy
   to wrap existing exception-throwing SDK methods.

5. **Service-layer patterns** — if your architecture has a service layer that returns
   `Result<T>` to controllers, using the same type for capability gating avoids
   type proliferation.

Skip FluentResults when:

- The capability check is on a **hot path** (hundreds of tools per request) —
  the heap allocations add up.
- You're building a **library**, not an application — adding a transitive dependency
  on FluentResults forces it on all consumers.
- You need **AOT/trimming** compatibility — FluentResults uses reflection internally.
- You only need **one error type** — FluentResults is overkill for `Value | Error`.

---

## 5. Approach C — Hybrid: Result with Optional Exception Re-throw

A pragmatic middle-ground: use `Result<T>` internally for composition, but provide an
`.Unwrap()` that throws if it's an error — letting callers choose their style.

```csharp
public readonly record struct Result<T>
{
    // ... same as Approach A ...

    /// <summary>
    /// Returns the value if success, or throws a CapabilityNotAvailableException
    /// containing the structured error data.
    /// </summary>
    public T Unwrap()
    {
        if (_isSuccess) return _value!;
        throw new CapabilityNotAvailableException(_error.Missing, _error.ToString());
    }

    /// <summary>
    /// Returns the value if success, or returns the provided fallback.
    /// </summary>
    public T UnwrapOr(T fallback) => _isSuccess ? _value! : fallback;

    /// <summary>
    /// Returns the value if success, or calls the fallback factory.
    /// </summary>
    public T UnwrapOrElse(Func<CapabilityError, T> fallback) =>
        _isSuccess ? _value! : fallback(_error);
}
```

```csharp
// ── Caller can choose: ──

// Option A: Handle explicitly (functional style)
result.Match(
    onSuccess: r => Process(r),
    onError: e => LogAndSkip(e));

// Option B: Unwrap at the boundary (exception style — familiar for most .NET devs)
try { var value = result.Unwrap(); Process(value); }
catch (CapabilityNotAvailableException) { /* ... */ }

// Option C: Fallback value (degraded operation)
var text = await TryCallTool("ai_summarize", reqs, args)
    .MapAsync(r => ((TextContentBlock)r.Content[0]).Text)
    .UnwrapOr("Summary not available — server lacks AI capability");
```

---

## 6. Comparison of Caller Code

### Same operation — four styles

```csharp
// ── GOAL: Call a tool, handle capability errors, return text or fallback ──

// ── Exception-Based ──
string GetResult1(CapabilityAwareMcpClient client, CancellationToken ct)
{
    try
    {
        var result = client.CallToolAsync("ai_summarize",
            new ServerCapabilityRequirements { Required = CapabilityFlag.Tools },
            new() { ["state"] = "WA" }, ct).Result;
        return ((TextContentBlock)result.Content[0]).Text;
    }
    catch (CapabilityNotAvailableException e)
    {
        Console.Error.WriteLine($"Missing: {e.Missing}");
        return "N/A";
    }
    catch (AggregateException ae) when (ae.InnerException is CapabilityNotAvailableException e2)
    {
        Console.Error.WriteLine($"Missing: {e2.Missing}");
        return "N/A";
    }
}

// ── Monadic (Custom Result) ──
async Task<string> GetResult2(MonadicCapabilityAwareMcpClient client, CancellationToken ct)
{
    var result = await client.TryCallToolAsync("ai_summarize",
        new ServerCapabilityRequirements { Required = CapabilityFlag.Tools },
        new() { ["state"] = "WA" }, ct);

    return result.Match(
        onSuccess: r => ((TextContentBlock)r.Content[0]).Text,
        onError: e =>
        {
            Console.Error.WriteLine($"Missing: {e.Missing}");
            return "N/A";
        });
}

// ── OneOf ──
async Task<string> GetResult3(OneOfCapabilityAwareMcpClient client, CancellationToken ct)
{
    var result = await client.TryCallToolAsync("ai_summarize",
        new ServerCapabilityRequirements { Required = CapabilityFlag.Tools },
        new() { ["state"] = "WA" }, ct);

    return result.Match(
        success: r => ((TextContentBlock)r.Content[0]).Text,
        error: e =>
        {
            Console.Error.WriteLine($"Missing: {e.Missing}");
            return "N/A";
        });
}
```

---

## 7. Chaining Multiple Operations

This is where monads shine. Compare chaining three capability-gated operations:

```csharp
// ── Exception-Based Chaining ──
async Task<string> ChainExceptions(CapabilityAwareMcpClient client, CancellationToken ct)
{
    try
    {
        // Step 1: list tools
        var tools = await client.ListToolsAsync(
            new ServerCapabilityRequirements { Required = CapabilityFlag.Tools },
            cancellationToken: ct);

        // Step 2: find and call a specific tool
        var tool = tools.First(t => t.Name == "get_alerts");
        var alerts = await client.CallToolAsync(tool.Name,
            new ServerCapabilityRequirements { Required = CapabilityFlag.Tools },
            new() { ["state"] = "WA" }, ct);

        // Step 3: list prompts for summarization
        var prompts = await client.ListPromptsAsync(
            new ServerCapabilityRequirements { Required = CapabilityFlag.Prompts },
            cancellationToken: ct);

        // Step 4: get a specific prompt
        var prompt = await client.GetPromptAsync("summarize",
            new ServerCapabilityRequirements { Required = CapabilityFlag.Prompts },
            new() { ["text"] = ((TextContentBlock)alerts.Content[0]).Text }, ct);

        return prompt.Messages[0].Content.Text ?? "";
    }
    catch (CapabilityNotAvailableException e)
    {
        return $"Unavailable: {e.Missing}";
    }
    catch (InvalidOperationException) // from First()
    {
        return "Tool not found";
    }
    catch (McpException e)
    {
        return $"Protocol error: {e.Message}";
    }
}

// ── Monadic Chaining (Custom Result) ──
async Task<Result<string>> ChainMonadic(
    MonadicCapabilityAwareMcpClient client, CancellationToken ct)
{
    return await client
        .TryListToolsAsync(
            new ServerCapabilityRequirements { Required = CapabilityFlag.Tools }, ct: ct)
        .BindAsync(tools =>
        {
            var tool = tools.FirstOrDefault(t => t.Name == "get_alerts");
            if (tool is null)
                return new CapabilityError { PrimitiveName = "get_alerts",
                    Message = "Tool not found on server" }
                    .AsAsyncResultError<CallToolResult>();

            return client.TryCallToolAsync(tool.Name,
                new ServerCapabilityRequirements { Required = CapabilityFlag.Tools },
                new() { ["state"] = "WA" }, ct);
        })
        .BindAsync(alerts =>
            client.TryListPromptsAsync(
                new ServerCapabilityRequirements { Required = CapabilityFlag.Prompts },
                ct: ct))
        .BindAsync(prompts =>
        {
            var prompt = prompts.FirstOrDefault(p => p.Name == "summarize");
            if (prompt is null)
                return new CapabilityError { PrimitiveName = "summarize",
                    Message = "Prompt not found on server" }
                    .AsAsyncResultError<GetPromptResult>();

            return client.TryGetPromptAsync("summarize",
                new ServerCapabilityRequirements { Required = CapabilityFlag.Prompts },
                // ... pass arguments
                , ct);
        })
        .MapAsync(promptResult =>
            promptResult.Messages[0].Content.Text ?? "");
}

// The caller handles the error once, at the end:
chainMonadicResult.Match(
    onSuccess: text => Console.WriteLine(text),
    onError: e => Console.Error.WriteLine($"Chain failed at '{e.PrimitiveName}': {e.Missing}"));
```

---

## 8. Recommendation

```
                ┌──────────────────────────────────────────────────┐
                │  RECOMMENDED APPROACH                            │
                │                                                  │
                │  Tier 1 (Library):  Custom Result<T> (Approach A)│
                │  Tier 2 (App):     FluentResults (Approach D)    │
                │                                                  │
                │  For the NuGet PACKAGE:                          │
                │  - Custom Result<T, CapabilityError> struct      │
                │  - Zero dependencies                            │
                │  - Value-type = no allocation on success         │
                │  - Unwrap() escape hatch for exception consumers │
                │  - Don't force FluentResults on package consumers│
                │                                                  │
                │  For APPLICATION code that USES the package:     │
                │  - If you already use FluentResults -> wrap the  │
                │    Result<T> into FluentResults at the boundary  │
                │  - If you don't -> use the Result<T> directly or │
                │    call .Unwrap() for exception-based flow       │
                │                                                  │
                │  Why NOT FluentResults in the package:           │
                │  - Adds transitive dependency for all consumers  │
                │  - Heap-allocates on every operation             │
                │  - Library consumers can't choose their monad    │
                │  - A single-error struct is sufficient here      │
                └──────────────────────────────────────────────────┘
```

The `Result<T>` + `Unwrap()` pattern gives you the best of both worlds:
- **Internally**: compose with `Map`/`Bind`/`Match` — no exceptions in the hot path
- **At the boundary**: call `.Unwrap()` to convert to exceptions if the caller prefers that style
- **Graceful degradation**: `.UnwrapOr(fallback)` lets the caller degrade without ceremony
- **Zero-cost abstraction**: the `Result<T>` struct is the size of two fields plus a bool — no heap allocation on success
- **Easy migration to FluentResults**: a 10-line adapter converts `Result<T>` to `FluentResults.Result<T>` for apps that want it:

```csharp
// Adapter: convert library Result<T> to FluentResults Result<T>
public static class ResultToFluentResultExtensions
{
    public static FluentResults.Result<T> ToFluentResult<T>(
        this Result<T> result)
    {
        return result.Match(
            onSuccess: value => FluentResults.Result.Ok(value),
            onError: error => FluentResults.Result.Fail<T>(
                new Error(error.ToString())));
    }
}
```

If in the future C# ships native union types, migrating from `Result<T>` to `T | CapabilityError`
would be a search-and-replace operation.
