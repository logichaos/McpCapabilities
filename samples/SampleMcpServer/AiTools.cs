using System.ComponentModel;

using McpCapabilities.Server;

using ModelContextProtocol.Server;

namespace SampleMcpServer;

[McpServerToolType]
public class AiTools
{
  [McpServerTool]
  [Description("summarize sent text and return it to caller")]
  [RequiredClientCapabilities(
      Required = CapabilityFlag.Sampling,
      Message = "Requires LLM sampling support")]
  public string AiSummarize(string text)
  {
    return $"Would summarize: {text}";
  }

  [McpServerTool]
  [Description("echo input text back")]
  public string Echo(string text)
  {
    return text;
  }
}