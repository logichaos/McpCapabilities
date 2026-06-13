# SampleMcpHttpClient

An MCP client sample that connects to the [SampleMcpServer](../SampleMcpServer/) over HTTP transport
and demonstrates capability-gated tool, prompt, and resource filtering.

## Build

```bash
dotnet build
```

## Run

### 1. Start the server in HTTP mode

```bash
dotnet run --project samples/SampleMcpServer/ -- MCP:Transport=http
```

The server starts on `http://localhost:5000` by default.

### 2. Run the HTTP client

```bash
dotnet run --project samples/SampleMcpHttpClient/
```

To specify a custom server URL:

```bash
dotnet run --project samples/SampleMcpHttpClient/ -- http://localhost:5123
```

## Expected Output

The client runs two capability profiles:

### FULL (Sampling + Roots + Elicitation)

The client advertises all three capabilities. The server returns all primitives:

```
--- Profile: FULL (Sampling + Roots + Elicitation) ---
  Tools (2):
    - ai_summarize
    - echo
  Prompts (2):
    - confirm_action
    - greeting
  Resources (2):
    - workspace_files
    - app_info
```

### MINIMAL (no capabilities)

The client advertises no capabilities. The server returns only ungated primitives:

```
--- Profile: MINIMAL (no capabilities) ---
  Tools (1):
    - echo
  Prompts (1):
    - greeting
  Resources (1):
    - app_info
```
