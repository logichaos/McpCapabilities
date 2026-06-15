using McpCapabilities.Server;

using ModelContextProtocol.Server;

namespace SampleMcpServer;

[McpServerToolType]
public class AiTools
{
  [McpServerTool]
  [RequiredClientCapabilities(
      Required = CapabilityFlag.Sampling,
      Message = "Requires LLM sampling support")]
  public string AiSummarize(string text)
  {
    return $"Would summarize: {text}";
  }

  [McpServerTool]
  public string Echo(string text)
  {
    return text;
  }
}