# SampleMcpClient

A minimal MCP client that connects to `SampleMcpServer` with different capability profiles to verify server-side capability gating.

## What it demonstrates

- **Full client** (Sampling + Roots + Elicitation) — expects all 6 primitives to be visible
- **Minimal client** (no capabilities) — expects only the 3 ungated primitives

## Prerequisites

Build the sample server first:

```bash
dotnet build samples/SampleMcpServer/
```

## Build

```bash
cd samples/SampleMcpClient
dotnet build
```

## Run

```bash
dotnet run
```

## Expected Output

```
=== MCP Sample Client ===

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

--- Profile: MINIMAL (no capabilities) ---
  Tools (1):
    - echo
  Prompts (1):
    - greeting
  Resources (1):
    - app_info

Done.
```

- **Full client sees 6 primitives** (2 tools + 2 prompts + 2 resources)
- **Minimal client sees 3 primitives** (1 ungated tool + 1 ungated prompt + 1 ungated resource)
- Gated primitives (`ai_summarize`, `confirm_action`, `workspace_files`) are hidden from the minimal client
