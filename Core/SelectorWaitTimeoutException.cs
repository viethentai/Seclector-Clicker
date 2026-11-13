using System.Globalization;

namespace AutoClickSelector.Core;

public sealed class SelectorWaitTimeoutException(int timeoutMilliseconds)
    : TimeoutException(
        AutoClickSelector.Services.Localization.F(
            "Không tìm thấy phần tử hiển thị trong {0} giây.",
            Math.Ceiling(timeoutMilliseconds / 1000d).ToString("0.#", CultureInfo.CurrentCulture)))
{
}
