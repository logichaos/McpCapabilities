## 1. Project conversion

- [ ] 1.1 Change `SampleMcpHttpClient.csproj` SDK from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web` and keep `ModelContextProtocol` package reference
- [ ] 1.2 Replace `Program.cs` console entry point with ASP.NET Core web host that registers Razor Pages and the MCP server URL via configuration

## 2. Layout and navigation

- [ ] 2.1 Create `Pages/_Layout.cshtml` with HTML5 boilerplate, a `<nav>` containing links to Dashboard (`/`), Full Profile (`/FullProfile`), and Minimal Profile (`/MinimalProfile`)
- [ ] 2.2 Create `Pages/_ViewStart.cshtml` referencing the shared layout
- [ ] 2.3 Create `Pages/_ViewImports.cshtml` with `@namespace SampleMcpHttpClient.Pages` and `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`

## 3. Dashboard page

- [ ] 3.1 Create `Pages/Index.cshtml` displaying connection status (connected/disconnected), server URL, and aggregate counts of tools/prompts/resources from an MCP client with full capabilities
- [ ] 3.2 Create `Pages/Index.cshtml.cs` PageModel that creates an `McpClient` via `HttpClientTransport`, calls list methods, and populates status and count properties with error handling for unreachable servers

## 4. Full Profile page

- [ ] 4.1 Create `Pages/FullProfile.cshtml` displaying three sections (Tools, Prompts, Resources) with lists of names, using full `ClientCapabilities`
- [ ] 4.2 Create `Pages/FullProfile.cshtml.cs` PageModel that creates an `McpClient` with `Sampling`, `Roots`, and `Elicitation` capabilities and populates typed lists

## 5. Minimal Profile page

- [ ] 5.1 Create `Pages/MinimalProfile.cshtml` displaying three sections (Tools, Prompts, Resources) with lists of names, using empty `ClientCapabilities`
- [ ] 5.2 Create `Pages/MinimalProfile.cshtml.cs` PageModel that creates an `McpClient` with empty capabilities and populates typed lists

## 6. Cleanup

- [ ] 6.1 Delete old console-only code (the existing `RunClientProfile` method and `Console.WriteLine`-based output)

## 7. Documentation

- [ ] 7.1 Update `README.md` with web app build/run instructions, server startup command, and the browser URL (`http://localhost:5001`)

## 8. Verification

- [ ] 8.1 Run `dotnet build` to confirm the project compiles without errors or warnings
- [ ] 8.2 Start the server in HTTP mode and run `dotnet run`, then verify all three pages load in a browser with correct capability-gated content
