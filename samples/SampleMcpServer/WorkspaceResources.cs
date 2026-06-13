using McpCapabilities.Server;
using ModelContextProtocol.Server;

namespace SampleMcpServer;

[McpServerResourceType]
public class WorkspaceResources
{
    [McpServerResource]
    [RequiredClientCapabilities(
        Required = CapabilityFlag.Roots,
        Message = "Requires filesystem root listing support")]
    public string WorkspaceFiles()
    {
        return "Contents of workspace://files would list the project file tree.";
    }

    [McpServerResource]
    public string AppInfo()
    {
        return """
               SampleMcpServer v1.0
               An MCP server demonstrating capability-gated tools, prompts, and resources.
               """;
    }
}
