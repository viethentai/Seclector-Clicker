namespace AutoClickSelector.Models;

/// <summary>Kết quả kiểm tra một CSS Selector trên trang hiện tại.</summary>
public sealed record SelectorValidationResult(
    bool IsValid,
    int MatchCount,
    int VisibleCount,
    string? Error);
