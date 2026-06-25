using FluentResults;

using ModelContextProtocol.Protocol;

using FrResult = FluentResults.Result;

namespace McpCapabilities.Server;

public static class CapabilityFilteringFluentExtensions
{
  public static Result<IList<Tool>> FilterByClientCapabilities(
      this IList<Tool> tools,
      ClientCapabilities? clientCaps,
      bool allowWhenNotProvided = true)
  {
    var visible = new List<Tool>();
    var hiddenCount = 0;

    foreach (var tool in tools)
    {
      var reqs = ClientCapabilityRequirements.ReadFromMeta(tool.Meta);

      if (CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allowWhenNotProvided))
      {
        visible.Add(tool);
      }
      else
      {
        hiddenCount++;
      }
    }

    if (visible.Count == 0 && hiddenCount > 0)
    {
      var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
      var aggregatedRequired = tools
          .Select(t => ClientCapabilityRequirements.ReadFromMeta(t.Meta).Required)
          .Aggregate(CapabilityFlag.None, (acc, r) => acc | r);

      var aggregatedMissing = aggregatedRequired & ~clientFlags;

      return FrResult.Fail<IList<Tool>>(
          new CapabilityNotMetError(
              aggregatedRequired,
              aggregatedMissing,
              "tools/list",
              $"None of the {tools.Count} tools are available to this client"));
    }

    return FrResult.Ok<IList<Tool>>(visible);
  }

  public static Result<IList<Prompt>> FilterByClientCapabilities(
      this IList<Prompt> prompts,
      ClientCapabilities? clientCaps,
      bool allowWhenNotProvided = true)
  {
    var visible = new List<Prompt>();
    var hiddenCount = 0;

    foreach (var prompt in prompts)
    {
      var reqs = ClientCapabilityRequirements.ReadFromMeta(prompt.Meta);

      if (CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allowWhenNotProvided))
      {
        visible.Add(prompt);
      }
      else
      {
        hiddenCount++;
      }
    }

    if (visible.Count == 0 && hiddenCount > 0)
    {
      var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
      var aggregatedRequired = prompts
          .Select(p => ClientCapabilityRequirements.ReadFromMeta(p.Meta).Required)
          .Aggregate(CapabilityFlag.None, (acc, r) => acc | r);

      var aggregatedMissing = aggregatedRequired & ~clientFlags;

      return FrResult.Fail<IList<Prompt>>(
          new CapabilityNotMetError(
              aggregatedRequired,
              aggregatedMissing,
              "prompts/list",
              $"None of the {prompts.Count} prompts are available to this client"));
    }

    return FrResult.Ok<IList<Prompt>>(visible);
  }

  public static Result<IList<Resource>> FilterByClientCapabilities(
      this IList<Resource> resources,
      ClientCapabilities? clientCaps,
      bool allowWhenNotProvided = true)
  {
    var visible = new List<Resource>();
    var hiddenCount = 0;

    foreach (var resource in resources)
    {
      var reqs = ClientCapabilityRequirements.ReadFromMeta(resource.Meta);

      if (CapabilityFlags.IsAllowed(reqs.Required, clientCaps, allowWhenNotProvided))
      {
        visible.Add(resource);
      }
      else
      {
        hiddenCount++;
      }
    }

    if (visible.Count == 0 && hiddenCount > 0)
    {
      var clientFlags = CapabilityFlags.FromClientCapabilities(clientCaps);
      var aggregatedRequired = resources
          .Select(r => ClientCapabilityRequirements.ReadFromMeta(r.Meta).Required)
          .Aggregate(CapabilityFlag.None, (acc, r) => acc | r);

      var aggregatedMissing = aggregatedRequired & ~clientFlags;

      return FrResult.Fail<IList<Resource>>(
          new CapabilityNotMetError(
              aggregatedRequired,
              aggregatedMissing,
              "resources/list",
              $"None of the {resources.Count} resources are available to this client"));
    }

    return FrResult.Ok<IList<Resource>>(visible);
  }
}
