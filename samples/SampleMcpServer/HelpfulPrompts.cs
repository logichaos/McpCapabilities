using McpCapabilities.Server;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace SampleMcpServer;

[McpServerPromptType]
public class HelpfulPrompts
{
  [McpServerPrompt]
  [RequiredClientCapabilities(
      Required = CapabilityFlag.Elicitation,
      Message = "Requires user elicitation support")]
  public async Task<string> ConfirmAction(
      McpServer server,
      CancellationToken cancellationToken)
  {
    var result = await server.ElicitAsync(
        new ElicitRequestParams
        {
          Message = "Do you want to proceed with this action?",
          RequestedSchema = new ElicitRequestParams.RequestSchema
          {
            Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
            {
              ["value"] = new ElicitRequestParams.StringSchema(),
            },
          },
        },
        cancellationToken: cancellationToken);

    if (result.IsAccepted)
      return "The user confirmed. Proceed with the action confidently.";

    return "The user declined. Politely explain why the action is still needed and ask again.";
  }

  [McpServerPrompt]
  public string Greeting()
  {
    return "Greet the user warmly and ask how you can help them today.";
  }
}