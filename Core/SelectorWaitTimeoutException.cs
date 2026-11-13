namespace AutoClickSelector.Core;

public sealed class SelectorWaitTimeoutException(int timeoutMilliseconds)
    : TimeoutException(
        $"Không tìm thấy phần tử hiển thị trong {Math.Ceiling(timeoutMilliseconds / 1000d):0.#} giây.")
{
}
