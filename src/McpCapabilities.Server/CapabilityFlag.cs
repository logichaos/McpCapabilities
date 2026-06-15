namespace McpCapabilities.Server;

[Flags]
public enum CapabilityFlag
{
  None = 0,
  Sampling = 1 << 0,
  Roots = 1 << 1,
  Elicitation = 1 << 2,
  ElicitationForm = 1 << 3,
  ElicitationUrl = 1 << 4,
  Tasks = 1 << 5,
  TaskList = 1 << 6,
  TaskCancel = 1 << 7,
  TaskAugmentedSampling = 1 << 8,
  TaskAugmentedElicitation = 1 << 9,
}