using System.ComponentModel;

using McpCapabilities.Server;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace SampleMcpServer;

[McpServerToolType]
public class AiTools
{
  [McpServerTool]
  [Description("summarize sent text using the client's LLM and return the result")]
  [RequiredClientCapabilities(
      Required = CapabilityFlag.Sampling,
      Message = "Requires LLM sampling support")]
  public async Task<string> AiSummarize(
      McpServer server,
      [Description("The text to summarize")] string text,
      CancellationToken cancellationToken)
  {
    if (server.ClientCapabilities?.Sampling is null)
      return "Client does not support sampling.";

    var result = await server.SampleAsync(
        new CreateMessageRequestParams
        {
          Messages =
          [
            new SamplingMessage
            {
              Role = Role.User,
              Content = [new TextContentBlock { Text = $"Please summarize the following text:\n\n{text}" }],
            }
          ],
          MaxTokens = 200,
          SystemPrompt = "You are a helpful assistant. Provide concise summaries.",
        },
        cancellationToken: cancellationToken);

    return result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text
        ?? "(no text response from LLM)";
  }

  [McpServerTool]
  [Description("echo input text back")]
  public string Echo(string text)
  {
    return text;
  }

  [McpServerTool]
  [Description("request information from the user via elicitation")]
  [RequiredClientCapabilities(
      Required = CapabilityFlag.Elicitation,
      Message = "Requires elicitation support")]
  public async Task<string> AiElicit(
      McpServer server,
      [Description("The message to show the user")] string message,
      CancellationToken cancellationToken)
  {
    if (server.ClientCapabilities?.Elicitation is null)
      return "Client does not support elicitation.";

    var result = await server.ElicitAsync(
        new ElicitRequestParams
        {
          Message = message,
          RequestedSchema = new ElicitRequestParams.RequestSchema
          {
            Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
            {
              ["value"] = new ElicitRequestParams.StringSchema(),
            },
          },
        },
        cancellationToken: cancellationToken);

    if (!result.IsAccepted || result.Content is null)
      return "User declined.";

    var userResponse = result.Content.TryGetValue("value", out var v)
        ? v.GetString() ?? "(null)"
        : "(no value)";

    return $"User responded: {userResponse}";
  }

  [McpServerTool]
  [Description("ask the user to pick from a set of choices via elicitation")]
  [RequiredClientCapabilities(
      Required = CapabilityFlag.Elicitation,
      Message = "Requires elicitation support")]
  public async Task<string> AiChoose(
      McpServer server,
      [Description("Comma-separated list of options")] string options,
      CancellationToken cancellationToken)
  {
    if (server.ClientCapabilities?.Elicitation is null)
      return "Client does not support elicitation.";

    var choices = options.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    var result = await server.ElicitAsync(
        new ElicitRequestParams
        {
          Message = "Choose one of the following options:",
          RequestedSchema = new ElicitRequestParams.RequestSchema
          {
            Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
            {
              ["value"] = new ElicitRequestParams.UntitledSingleSelectEnumSchema
              {
                Description = "Pick an option",
                Enum = choices.ToList(),
              },
            },
          },
        },
        cancellationToken: cancellationToken);

    if (!result.IsAccepted || result.Content is null)
      return "User declined to choose.";

    var choice = result.Content.TryGetValue("value", out var v)
        ? v.GetString() ?? ""
        : "";

    return $"User chose: {choice}";
  }
}