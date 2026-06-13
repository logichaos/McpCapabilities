using McpCapabilities.Server;
using ModelContextProtocol.Server;

namespace SampleMcpServer;

[McpServerPromptType]
public class HelpfulPrompts
{
    [McpServerPrompt]
    [RequiredClientCapabilities(
        Required = CapabilityFlag.Elicitation,
        Message = "Requires user elicitation support")]
    public string ConfirmAction()
    {
        return "Ask the user to confirm the action before proceeding. "
            + "If they decline, explain the consequences and ask again.";
    }

    [McpServerPrompt]
    public string Greeting()
    {
        return "Greet the user warmly and ask how you can help them today.";
    }
}
