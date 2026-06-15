using McpCapabilities.Server;

using ModelContextProtocol.Protocol;

namespace McpCapabilities.Server.Unit.Tests;

#pragma warning disable MCPEXP001

public class CapabilityFlagsFromClientCapabilitiesTests
{
  [Test]
  public async Task FromClientCapabilities_NullClient_ReturnsNone()
  {
    var result = CapabilityFlags.FromClientCapabilities(null);

    await Assert.That(result).IsEqualTo(CapabilityFlag.None);
  }

  [Test]
  public async Task FromClientCapabilities_SamplingOnly_ReturnsSampling()
  {
    var caps = new ClientCapabilities
    {
      Sampling = new SamplingCapability(),
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result).IsEqualTo(CapabilityFlag.Sampling);
  }

  [Test]
  public async Task FromClientCapabilities_RootsOnly_ReturnsRoots()
  {
    var caps = new ClientCapabilities
    {
      Roots = new RootsCapability(),
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result).IsEqualTo(CapabilityFlag.Roots);
  }

  [Test]
  public async Task FromClientCapabilities_ElicitationWithoutSubCapabilities_ReturnsElicitationOnly()
  {
    var caps = new ClientCapabilities
    {
      Elicitation = new ElicitationCapability(),
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result).IsEqualTo(CapabilityFlag.Elicitation);
  }

  [Test]
  public async Task FromClientCapabilities_ElicitationWithFormOnly_ReturnsElicitationAndForm()
  {
    var caps = new ClientCapabilities
    {
      Elicitation = new ElicitationCapability
      {
        Form = new FormElicitationCapability(),
      },
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result.HasFlag(CapabilityFlag.Elicitation)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.ElicitationForm)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.ElicitationUrl)).IsFalse();
  }

  [Test]
  public async Task FromClientCapabilities_ElicitationWithUrlOnly_ReturnsElicitationAndUrl()
  {
    var caps = new ClientCapabilities
    {
      Elicitation = new ElicitationCapability
      {
        Url = new UrlElicitationCapability(),
      },
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result.HasFlag(CapabilityFlag.Elicitation)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.ElicitationUrl)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.ElicitationForm)).IsFalse();
  }

  [Test]
  public async Task FromClientCapabilities_ElicitationWithBothFormAndUrl_ReturnsAllElicitationFlags()
  {
    var caps = new ClientCapabilities
    {
      Elicitation = new ElicitationCapability
      {
        Form = new FormElicitationCapability(),
        Url = new UrlElicitationCapability(),
      },
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result.HasFlag(CapabilityFlag.Elicitation)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.ElicitationForm)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.ElicitationUrl)).IsTrue();
  }

  [Test]
  public async Task FromClientCapabilities_TasksWithoutSubCapabilities_ReturnsTasksOnly()
  {
    var caps = new ClientCapabilities
    {
      Tasks = new McpTasksCapability(),
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result).IsEqualTo(CapabilityFlag.Tasks);
  }

  [Test]
  public async Task FromClientCapabilities_TasksWithListAndCancel_ReturnsTasksListAndCancel()
  {
    var caps = new ClientCapabilities
    {
      Tasks = new McpTasksCapability
      {
        List = new ListMcpTasksCapability(),
        Cancel = new CancelMcpTasksCapability(),
      },
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result.HasFlag(CapabilityFlag.Tasks)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.TaskList)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.TaskCancel)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.TaskAugmentedSampling)).IsFalse();
    await Assert.That(result.HasFlag(CapabilityFlag.TaskAugmentedElicitation)).IsFalse();
  }

  [Test]
  public async Task FromClientCapabilities_FullTasks_ReturnsAllTaskFlags()
  {
    var caps = new ClientCapabilities
    {
      Tasks = new McpTasksCapability
      {
        List = new ListMcpTasksCapability(),
        Cancel = new CancelMcpTasksCapability(),
        Requests = new RequestMcpTasksCapability
        {
          Sampling = new SamplingMcpTasksCapability
          {
            CreateMessage = new CreateMessageMcpTasksCapability(),
          },
          Elicitation = new ElicitationMcpTasksCapability
          {
            Create = new CreateElicitationMcpTasksCapability(),
          },
        },
      },
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result.HasFlag(CapabilityFlag.Tasks)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.TaskList)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.TaskCancel)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.TaskAugmentedSampling)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.TaskAugmentedElicitation)).IsTrue();
  }

  [Test]
  public async Task FromClientCapabilities_CombinedCapabilities_ReturnsCombined()
  {
    var caps = new ClientCapabilities
    {
      Sampling = new SamplingCapability(),
      Roots = new RootsCapability(),
      Elicitation = new ElicitationCapability
      {
        Form = new FormElicitationCapability(),
      },
    };

    var result = CapabilityFlags.FromClientCapabilities(caps);

    await Assert.That(result.HasFlag(CapabilityFlag.Sampling)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.Roots)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.Elicitation)).IsTrue();
    await Assert.That(result.HasFlag(CapabilityFlag.ElicitationForm)).IsTrue();
  }
}