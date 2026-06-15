using System.Text.Json.Nodes;

using FluentResults;

using McpCapabilities.Server;

using ModelContextProtocol.Protocol;

namespace McpCapabilities.Server.Unit.Tests;

public class CapabilityFilteringFluentExtensionsTests
{
  private static Tool CreateTool(string name, CapabilityFlag? required = null, string? message = null)
  {
    var tool = new Tool { Name = name };

    if (required.HasValue && required.Value != CapabilityFlag.None)
    {
      var reqs = new ClientCapabilityRequirements
      {
        Required = required.Value,
        Message = message,
      };
      tool.Meta ??= [];
      reqs.WriteToMeta(tool.Meta);
    }

    return tool;
  }

  private static Prompt CreatePrompt(string name, CapabilityFlag? required = null)
  {
    var prompt = new Prompt { Name = name };

    if (required.HasValue && required.Value != CapabilityFlag.None)
    {
      var reqs = new ClientCapabilityRequirements { Required = required.Value };
      prompt.Meta ??= [];
      reqs.WriteToMeta(prompt.Meta);
    }

    return prompt;
  }

  [Test]
  public async Task FilterByClientCapabilities_MixedVisibleHidden_ReturnsOnlyVisible()
  {
    var tools = new List<Tool>
        {
            CreateTool("sampling_tool", CapabilityFlag.Sampling),
            CreateTool("elicitation_tool", CapabilityFlag.Elicitation),
            CreateTool("no_reqs_tool"),
        };
    var clientCaps = new ClientCapabilities { Sampling = new SamplingCapability() };

    var result = tools.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(2);
    await Assert.That(result.Value.Select(t => t.Name)).Contains("sampling_tool");
    await Assert.That(result.Value.Select(t => t.Name)).Contains("no_reqs_tool");
    await Assert.That(result.Value.Select(t => t.Name)).DoesNotContain("elicitation_tool");
  }

  [Test]
  public async Task FilterByClientCapabilities_AllHidden_ReturnsFailure()
  {
    var tools = new List<Tool>
        {
            CreateTool("sampling_tool", CapabilityFlag.Sampling),
            CreateTool("roots_tool", CapabilityFlag.Roots),
        };
    var clientCaps = new ClientCapabilities();

    var result = tools.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsFailed).IsTrue();
    await Assert.That(result.Errors).Count().IsEqualTo(1);
    await Assert.That(result.Errors[0]).IsTypeOf<CapabilityNotMetError>();
  }

  [Test]
  public async Task FilterByClientCapabilities_EmptyList_ReturnsSuccessWithEmpty()
  {
    var tools = new List<Tool>();
    var clientCaps = new ClientCapabilities { Sampling = new SamplingCapability() };

    var result = tools.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(0);
  }

  [Test]
  public async Task FilterByClientCapabilities_HiddenTools_AreExcluded()
  {
    var tools = new List<Tool>
        {
            CreateTool("sampling_tool", CapabilityFlag.Sampling),
            CreateTool("roots_tool", CapabilityFlag.Roots),
            CreateTool("no_reqs_tool"),
        };
    var clientCaps = new ClientCapabilities { Sampling = new SamplingCapability() };

    var result = tools.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(2);
    await Assert.That(result.Value.Select(t => t.Name)).Contains("sampling_tool");
    await Assert.That(result.Value.Select(t => t.Name)).Contains("no_reqs_tool");
    await Assert.That(result.Value.Select(t => t.Name)).DoesNotContain("roots_tool");
  }

  [Test]
  public async Task FilterByClientCapabilities_NullClient_AllAnnotatedHidden()
  {
    var tools = new List<Tool>
        {
            CreateTool("sampling_tool", CapabilityFlag.Sampling),
            CreateTool("no_reqs_tool"),
        };

    var result = tools.FilterByClientCapabilities(null);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(1);
    await Assert.That(result.Value[0].Name).IsEqualTo("no_reqs_tool");
  }

  [Test]
  public async Task FilterByClientCapabilities_FullClient_ShowsAll()
  {
    var tools = new List<Tool>
        {
            CreateTool("sampling_tool", CapabilityFlag.Sampling),
            CreateTool("roots_tool", CapabilityFlag.Roots),
            CreateTool("elicitation_tool", CapabilityFlag.Elicitation),
            CreateTool("no_reqs_tool"),
        };
    var clientCaps = new ClientCapabilities
    {
      Sampling = new SamplingCapability(),
      Roots = new RootsCapability(),
      Elicitation = new ElicitationCapability(),
    };

    var result = tools.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(4);
  }

  [Test]
  public async Task FilterByClientCapabilities_AllHidden_ErrorCarriesAggregatedFlags()
  {
    var tools = new List<Tool>
        {
            CreateTool("sampling_tool", CapabilityFlag.Sampling),
            CreateTool("roots_tool", CapabilityFlag.Roots),
        };
    var clientCaps = new ClientCapabilities();

    var result = tools.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsFailed).IsTrue();
    var error = (CapabilityNotMetError)result.Errors[0];
    await Assert.That(error.Required).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Roots);
    await Assert.That(error.Missing).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Roots);
    await Assert.That(error.PrimitiveName).IsEqualTo("tools/list");
    await Assert.That(error.Message).Contains("None of the 2 tools");
  }

  [Test]
  public async Task FilterByClientCapabilities_AllHiddenNullClient_FailsWithError()
  {
    var tools = new List<Tool>
        {
            CreateTool("sampling_tool", CapabilityFlag.Sampling),
            CreateTool("roots_tool", CapabilityFlag.Roots),
        };

    var result = tools.FilterByClientCapabilities(null);

    await Assert.That(result.IsFailed).IsTrue();
    var error = (CapabilityNotMetError)result.Errors[0];
    await Assert.That(error.Missing).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Roots);
    await Assert.That(error.PrimitiveName).IsEqualTo("tools/list");
  }

  [Test]
  public async Task FilterByClientCapabilities_AllHiddenPartialSatisfaction_ErrorShowsMissingOnly()
  {
    var tools = new List<Tool>
        {
            CreateTool("sampling_plus_roots", CapabilityFlag.Sampling | CapabilityFlag.Roots),
            CreateTool("sampling_plus_elicitation", CapabilityFlag.Sampling | CapabilityFlag.Elicitation),
        };
    var clientCaps = new ClientCapabilities { Sampling = new SamplingCapability() };

    var result = tools.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsFailed).IsTrue();
    var error = (CapabilityNotMetError)result.Errors[0];
    await Assert.That(error.Required).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Roots | CapabilityFlag.Elicitation);
    await Assert.That(error.Missing).IsEqualTo(CapabilityFlag.Roots | CapabilityFlag.Elicitation);
  }

  [Test]
  public async Task FilterByClientCapabilities_Prompts_FiltersCorrectly()
  {
    var prompts = new List<Prompt>
        {
            CreatePrompt("sampling_prompt", CapabilityFlag.Sampling),
            CreatePrompt("no_reqs_prompt"),
        };
    var clientCaps = new ClientCapabilities();

    var result = prompts.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(1);
    await Assert.That(result.Value[0].Name).IsEqualTo("no_reqs_prompt");
  }

  [Test]
  public async Task FilterByClientCapabilities_Prompts_AllHidden_ReturnsFailure()
  {
    var prompts = new List<Prompt>
        {
            CreatePrompt("sampling_prompt", CapabilityFlag.Sampling),
            CreatePrompt("elicitation_prompt", CapabilityFlag.Elicitation),
        };
    var clientCaps = new ClientCapabilities();

    var result = prompts.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsFailed).IsTrue();
    await Assert.That(result.Errors).Count().IsEqualTo(1);
    await Assert.That(result.Errors[0]).IsTypeOf<CapabilityNotMetError>();
    var error = (CapabilityNotMetError)result.Errors[0];
    await Assert.That(error.PrimitiveName).IsEqualTo("prompts/list");
    await Assert.That(error.Message).Contains("None of the 2 prompts");
  }

  [Test]
  public async Task FilterByClientCapabilities_Prompts_AllHidden_ErrorCarriesAggregatedFlags()
  {
    var prompts = new List<Prompt>
        {
            CreatePrompt("sampling_prompt", CapabilityFlag.Sampling),
            CreatePrompt("roots_prompt", CapabilityFlag.Roots),
        };
    var clientCaps = new ClientCapabilities();

    var result = prompts.FilterByClientCapabilities(clientCaps);

    var error = (CapabilityNotMetError)result.Errors[0];
    await Assert.That(error.Required).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Roots);
    await Assert.That(error.Missing).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Roots);
  }

  [Test]
  public async Task FilterByClientCapabilities_Prompts_EmptyList_ReturnsSuccessWithEmpty()
  {
    var prompts = new List<Prompt>();
    var clientCaps = new ClientCapabilities { Sampling = new SamplingCapability() };

    var result = prompts.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(0);
  }

  [Test]
  public async Task FilterByClientCapabilities_Prompts_NullClient_AllAnnotatedHidden()
  {
    var prompts = new List<Prompt>
        {
            CreatePrompt("sampling_prompt", CapabilityFlag.Sampling),
            CreatePrompt("no_reqs_prompt"),
        };

    var result = prompts.FilterByClientCapabilities(null);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(1);
    await Assert.That(result.Value[0].Name).IsEqualTo("no_reqs_prompt");
  }

  [Test]
  public async Task FilterByClientCapabilities_Prompts_NullClient_AllAnnotated_AllHidden_ReturnsFailure()
  {
    var prompts = new List<Prompt>
        {
            CreatePrompt("sampling_prompt", CapabilityFlag.Sampling),
        };

    var result = prompts.FilterByClientCapabilities(null);

    await Assert.That(result.IsFailed).IsTrue();
    var error = (CapabilityNotMetError)result.Errors[0];
    await Assert.That(error.PrimitiveName).IsEqualTo("prompts/list");
    await Assert.That(error.Message).Contains("None of the 1 prompts");
  }

  [Test]
  public async Task FilterByClientCapabilities_Prompts_FullClient_ShowsAll()
  {
    var prompts = new List<Prompt>
        {
            CreatePrompt("sampling_prompt", CapabilityFlag.Sampling),
            CreatePrompt("roots_prompt", CapabilityFlag.Roots),
            CreatePrompt("no_reqs_prompt"),
        };
    var clientCaps = new ClientCapabilities
    {
      Sampling = new SamplingCapability(),
      Roots = new RootsCapability(),
    };

    var result = prompts.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(3);
  }

  [Test]
  public async Task FilterByClientCapabilities_Resources_FiltersCorrectly()
  {
    var resources = new List<Resource>
        {
            new() { Name = "roots_resource", Uri = "resource://roots", Meta = CreateMetaWithRequirements(CapabilityFlag.Roots) },
            new() { Name = "no_reqs_resource", Uri = "resource://noreqs" },
        };
    var clientCaps = new ClientCapabilities { Roots = new RootsCapability() };

    var result = resources.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(2);
  }

  [Test]
  public async Task FilterByClientCapabilities_Resources_AllHidden_ReturnsFailure()
  {
    var resources = new List<Resource>
        {
            new() { Name = "sampling_resource", Uri = "resource://sampling", Meta = CreateMetaWithRequirements(CapabilityFlag.Sampling) },
            new() { Name = "roots_resource", Uri = "resource://roots", Meta = CreateMetaWithRequirements(CapabilityFlag.Roots) },
        };
    var clientCaps = new ClientCapabilities();

    var result = resources.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsFailed).IsTrue();
    await Assert.That(result.Errors).Count().IsEqualTo(1);
    await Assert.That(result.Errors[0]).IsTypeOf<CapabilityNotMetError>();
    var error = (CapabilityNotMetError)result.Errors[0];
    await Assert.That(error.PrimitiveName).IsEqualTo("resources/list");
    await Assert.That(error.Message).Contains("None of the 2 resources");
  }

  [Test]
  public async Task FilterByClientCapabilities_Resources_AllHidden_ErrorCarriesAggregatedFlags()
  {
    var resources = new List<Resource>
        {
            new() { Name = "sampling_resource", Uri = "resource://sampling", Meta = CreateMetaWithRequirements(CapabilityFlag.Sampling) },
            new() { Name = "roots_resource", Uri = "resource://roots", Meta = CreateMetaWithRequirements(CapabilityFlag.Roots) },
        };
    var clientCaps = new ClientCapabilities();

    var result = resources.FilterByClientCapabilities(clientCaps);

    var error = (CapabilityNotMetError)result.Errors[0];
    await Assert.That(error.Required).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Roots);
    await Assert.That(error.Missing).IsEqualTo(CapabilityFlag.Sampling | CapabilityFlag.Roots);
  }

  [Test]
  public async Task FilterByClientCapabilities_Resources_EmptyList_ReturnsSuccessWithEmpty()
  {
    var resources = new List<Resource>();
    var clientCaps = new ClientCapabilities { Sampling = new SamplingCapability() };

    var result = resources.FilterByClientCapabilities(clientCaps);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(0);
  }

  [Test]
  public async Task FilterByClientCapabilities_Resources_NullClient_AllAnnotatedHidden()
  {
    var resources = new List<Resource>
        {
            new() { Name = "sampling_resource", Uri = "resource://sampling", Meta = CreateMetaWithRequirements(CapabilityFlag.Sampling) },
            new() { Name = "no_reqs_resource", Uri = "resource://noreqs" },
        };

    var result = resources.FilterByClientCapabilities(null);

    await Assert.That(result.IsSuccess).IsTrue();
    await Assert.That(result.Value).Count().IsEqualTo(1);
    await Assert.That(result.Value[0].Name).IsEqualTo("no_reqs_resource");
  }

  [Test]
  public async Task FilterByClientCapabilities_Resources_NullClient_AllAnnotated_AllHidden_ReturnsFailure()
  {
    var resources = new List<Resource>
        {
            new() { Name = "roots_resource", Uri = "resource://roots", Meta = CreateMetaWithRequirements(CapabilityFlag.Roots) },
        };

    var result = resources.FilterByClientCapabilities(null);

    await Assert.That(result.IsFailed).IsTrue();
    var error = (CapabilityNotMetError)result.Errors[0];
    await Assert.That(error.PrimitiveName).IsEqualTo("resources/list");
    await Assert.That(error.Message).Contains("None of the 1 resources");
  }

  private static JsonObject CreateMetaWithRequirements(CapabilityFlag flags)
  {
    var meta = new JsonObject();
    new ClientCapabilityRequirements { Required = flags }.WriteToMeta(meta);
    return meta;
  }
}