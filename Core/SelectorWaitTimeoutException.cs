namespace AutoClickSelector.Core;

/// <summary>Báo rằng Selector không hiển thị trong thời gian chờ cho phép.</summary>
public sealed class SelectorWaitTimeoutException(int timeoutMilliseconds)
    : TimeoutException(
        $"Không tìm thấy phần tử hiển thị trong {Math.Ceiling(timeoutMilliseconds / 1000d):0.#} giây.")
{
}
