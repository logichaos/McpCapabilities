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
}