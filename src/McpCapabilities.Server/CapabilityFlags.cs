using ModelContextProtocol.Protocol;

namespace McpCapabilities.Server;

public static class CapabilityFlags
{
  public static bool IsSatisfied(CapabilityFlag required, CapabilityFlag available)
      => (available & required) == required;

  public static CapabilityFlag FromClientCapabilities(ClientCapabilities? caps)
  {
    if (caps is null)
      return CapabilityFlag.None;

    var flags = CapabilityFlag.None;

    if (caps.Sampling is not null)
      flags |= CapabilityFlag.Sampling;

    if (caps.Roots is not null)
      flags |= CapabilityFlag.Roots;

    if (caps.Elicitation is not null)
    {
      flags |= CapabilityFlag.Elicitation;

      if (caps.Elicitation.Form is not null)
        flags |= CapabilityFlag.ElicitationForm;

      if (caps.Elicitation.Url is not null)
        flags |= CapabilityFlag.ElicitationUrl;
    }

#pragma warning disable MCPEXP001 // Tasks capability is experimental
    if (caps.Tasks is not null)
    {
      flags |= CapabilityFlag.Tasks;

      if (caps.Tasks.List is not null)
        flags |= CapabilityFlag.TaskList;

      if (caps.Tasks.Cancel is not null)
        flags |= CapabilityFlag.TaskCancel;

      if (caps.Tasks.Requests?.Sampling?.CreateMessage is not null)
        flags |= CapabilityFlag.TaskAugmentedSampling;

      if (caps.Tasks.Requests?.Elicitation?.Create is not null)
        flags |= CapabilityFlag.TaskAugmentedElicitation;
    }
#pragma warning restore MCPEXP001

    return flags;
  }
}