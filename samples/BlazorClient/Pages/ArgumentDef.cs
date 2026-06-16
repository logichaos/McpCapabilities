namespace BlazorClient.Pages;

public sealed record ArgumentDef(
    string Name,
    string? Description = null,
    string Type = "any",
    bool Required = false);