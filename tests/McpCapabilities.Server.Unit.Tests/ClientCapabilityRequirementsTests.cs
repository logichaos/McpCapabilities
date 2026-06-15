using System.Text.Json.Nodes;

using McpCapabilities.Server;

using ModelContextProtocol.Protocol;

namespace McpCapabilities.Server.Unit.Tests;

public class ClientCapabilityRequirementsTests
{
  [Test]
  public async Task None_HasNoRequirements()
  {
    var none = ClientCapabilityRequirements.None;

    await Assert.That(none.Required).IsEqualTo(CapabilityFlag.None);
    await Assert.That(none.Message).IsNull();
  }

  [Test]
  public async Task CreateInstance_SetsProperties()
  {
    var reqs = new ClientCapabilityRequirements
    {
      Required = CapabilityFlag.Sampling,
      Message = "Needs LLM",
    };

    await Assert.That(reqs.Required).IsEqualTo(CapabilityFlag.Sampling);
    await Assert.That(reqs.Message).IsEqualTo("Needs LLM");
  }

  [Test]
  public async Task WriteToMeta_WithFlagsAndMessage_WritesJson()
  {
    var reqs = new ClientCapabilityRequirements
    {
      Required = CapabilityFlag.Sampling | CapabilityFlag.Elicitation,
      Message = "Needs LLM and confirmation",
    };
    var meta = new JsonObject();

    reqs.WriteToMeta(meta);

    var capNode = meta["__mcp_capabilities_required"];
    await Assert.That(capNode).IsNotNull();
    await Assert.That(capNode!["flags"]!.GetValue<string>()).IsEqualTo("Sampling, Elicitation");
    await Assert.That(capNode["message"]!.GetValue<string>()).IsEqualTo("Needs LLM and confirmation");
  }

  [Test]
  public async Task WriteToMeta_WithNoMessage_WritesFlagsWithoutMessageValue()
  {
    var reqs = new ClientCapabilityRequirements
    {
      Required = CapabilityFlag.Sampling,
      Message = null,
    };
    var meta = new JsonObject();

    reqs.WriteToMeta(meta);

    var capNode = meta["__mcp_capabilities_required"];
    await Assert.That(capNode).IsNotNull();
    await Assert.That(capNode!["flags"]!.GetValue<string>()).IsEqualTo("Sampling");
    var messageValue = capNode["message"]?.GetValue<string>();
    await Assert.That(messageValue).IsNull();
  }

  [Test]
  public async Task ReadFromMeta_PopulatedMeta_ReturnsRequirements()
  {
    var meta = new JsonObject
    {
      ["__mcp_capabilities_required"] = new JsonObject
      {
        ["flags"] = "Sampling, Elicitation",
        ["message"] = "test",
      },
    };

    var result = ClientCapabilityRequirements.ReadFromMeta(meta);

    await Assert.That(result.Required).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Elicitation);
    await Assert.That(result.Message).IsEqualTo("test");
  }

  [Test]
  public async Task ReadFromMeta_NullMeta_ReturnsNone()
  {
    var result = ClientCapabilityRequirements.ReadFromMeta(null);

    await Assert.That(result.Required).IsEqualTo(CapabilityFlag.None);
    await Assert.That(result.Message).IsNull();
  }

  [Test]
  public async Task ReadFromMeta_EmptyMeta_ReturnsNone()
  {
    var meta = new JsonObject();

    var result = ClientCapabilityRequirements.ReadFromMeta(meta);

    await Assert.That(result.Required).IsEqualTo(CapabilityFlag.None);
  }

  [Test]
  public async Task ReadFromMeta_MissingKey_ReturnsNone()
  {
    var meta = new JsonObject
    {
      ["other_key"] = "value",
    };

    var result = ClientCapabilityRequirements.ReadFromMeta(meta);

    await Assert.That(result.Required).IsEqualTo(CapabilityFlag.None);
  }

  [Test]
  public async Task IsSatisfiedBy_AllRequirementsMet_ReturnsTrue()
  {
    var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Sampling };
    var caps = new ClientCapabilities { Sampling = new SamplingCapability() };

    var result = reqs.IsSatisfiedBy(caps);

    await Assert.That(result).IsTrue();
  }

  [Test]
  public async Task IsSatisfiedBy_PartialRequirementsMet_ReturnsFalse()
  {
    var reqs = new ClientCapabilityRequirements
    {
      Required = CapabilityFlag.Sampling | CapabilityFlag.Elicitation,
    };
    var caps = new ClientCapabilities { Sampling = new SamplingCapability() };

    var result = reqs.IsSatisfiedBy(caps);

    await Assert.That(result).IsFalse();
  }

  [Test]
  public async Task IsSatisfiedBy_NoneRequired_ReturnsTrue()
  {
    var reqs = ClientCapabilityRequirements.None;

    var result = reqs.IsSatisfiedBy(null);

    await Assert.That(result).IsTrue();
  }

  [Test]
  public async Task IsSatisfiedBy_NullCapabilities_ReturnsFalse()
  {
    var reqs = new ClientCapabilityRequirements { Required = CapabilityFlag.Sampling };

    var result = reqs.IsSatisfiedBy(null);

    await Assert.That(result).IsFalse();
  }
}