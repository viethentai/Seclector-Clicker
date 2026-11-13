namespace AutoClickSelector.Models;

public sealed record SelectorValidationResult(
    bool IsValid,
    int MatchCount,
    int VisibleCount,
    string? Error);
