using System.Globalization;

namespace AutoClickSelector.Services;

public static class Localization
{
    public const string Vietnamese = "vi";
    public const string English = "en";

    private static readonly Dictionary<string, string> Translations = new(StringComparer.Ordinal)
    {
        ["Tự động thao tác trên trình duyệt"] = "Automate actions in your browser",
        ["Chế độ tối"] = "Dark mode",
        ["Chế độ sáng"] = "Light mode",
        ["Ngôn ngữ"] = "Language",

        ["1. Trình duyệt"] = "1. Browser",
        ["Mở trình duyệt"] = "Open browser",
        ["Quét tab"] = "Scan tabs",
        ["Tab đang điều khiển"] = "Controlled tab",
        ["Chọn trình duyệt cần mở"] = "Browser to open",
        ["Chọn trình duyệt"] = "Choose browser",
        ["Trình duyệt khác"] = "Other browser",
        ["Mở trình duyệt đã chọn với cổng điều khiển. Nếu đang mở sẵn với cổng đó thì chỉ kết nối lại."] =
            "Open the selected browser with the control port. If it is already open with that port, just reconnect.",
        ["Tìm các tab đang mở để chọn tab cần điều khiển."] = "Find open tabs to pick the one to control.",
        ["Chọn đường dẫn tệp .exe của trình duyệt không có trong danh sách."] =
            "Pick the .exe of a browser that is not in the list.",

        ["2. Selector"] = "2. Selector",
        ["Selector đã lưu"] = "Saved selectors",
        ["Tên gợi nhớ, ví dụ: Nút video tiếp theo"] = "Friendly name, e.g. Next video button",
        ["Lưu"] = "Save",
        ["Xóa"] = "Delete",
        ["CSS Selector"] = "CSS Selector",
        ["Ví dụ: button[data-e2e='arrow-right']"] = "e.g. button[data-e2e='arrow-right']",
        ["Chọn phần tử"] = "Pick element",
        ["Kiểm tra"] = "Validate",
        ["Xem chữ hiển thị"] = "Show visible text",
        ["Chuyển sang trình duyệt, bấm vào phần tử cần chọn. Nhấn Esc để hủy."] =
            "Switch to the browser and click the element to pick. Press Esc to cancel.",
        ["Liệt kê chữ đang hiển thị trong khung nhìn, giúp tìm từ khóa quảng cáo."] =
            "List the text visible in the viewport to help find ad keywords.",

        ["3. Chạy thao tác"] = "3. Run",
        ["Khoảng nghỉ giữa các lần click"] = "Delay between clicks",
        ["Thời gian chờ tối đa để Selector hiển thị (giây)"] = "Max wait for the selector to appear (seconds)",
        ["giây"] = "seconds",
        ["phút"] = "minutes",
        ["Bắt đầu (F6)"] = "Start (F6)",
        ["Dừng (F7)"] = "Stop (F7)",
        ["Click 1 lần (F8)"] = "Click once (F8)",
        ["Làm mới trang"] = "Reload page",

        ["4. Click khi có từ khóa"] = "4. Click on keyword",
        ["Từ khóa, cách nhau bằng dấu phẩy"] = "Keywords, separated by commas",
        ["Ví dụ: Sponsored, Quảng cáo"] = "e.g. Sponsored, Advertisement",
        ["Bật theo dõi từ khóa"] = "Watch for keywords",
        ["Khi thấy từ khóa trên màn hình, ứng dụng bấm Selector ở mục 2 để chuyển sang video tiếp theo. Chỉ tính chữ đang hiển thị trong khung nhìn."] =
            "When a keyword is visible on screen, the app clicks the selector from section 2 to go to the next video. Only text inside the viewport counts.",

        ["5. Cài đặt"] = "5. Settings",
        ["Thu nhỏ xuống khay khi chạy"] = "Minimize to tray",
        ["Thoát xuống khay khi bấm X"] = "Close button hides to tray",
        ["Thông báo khi dừng hoặc lỗi"] = "Notify on stop or error",
        ["Phím tắt toàn cục (F6/F7/F8)"] = "Global hotkeys (F6/F7/F8)",
        ["Bỏ qua click trùng khi phần tử không đổi"] = "Skip duplicate clicks when the element is unchanged",

        ["Nhật ký hoạt động"] = "Activity log",
        ["Tìm trong nhật ký..."] = "Search the log...",
        ["Xuất"] = "Export",
        ["Xoá nhật ký"] = "Clear log",
        ["Xuất nhật ký"] = "Export log",
        ["Văn bản"] = "Text",
        ["Tất cả"] = "All",
        ["Lỗi"] = "Errors",
        ["Cảnh báo"] = "Warnings",
        ["Thành công"] = "Success",
        ["Thông tin"] = "Info",
        ["Đang xử lý"] = "Processing",
        ["THÀNH CÔNG"] = "SUCCESS",
        ["CẢNH BÁO"] = "WARNING",
        ["LỖI"] = "ERROR",
        ["ĐANG XỬ LÝ"] = "PROCESSING",
        ["THÔNG TIN"] = "INFO",

        ["● Sẵn sàng"] = "● Ready",
        ["● Đã kết nối"] = "● Connected",
        ["● Chưa kết nối"] = "● Not connected",
        ["Hãy quét tab và chọn một tab."] = "Scan tabs and pick one.",
        ["Mở trình duyệt hoặc quét tab để kết nối."] = "Open the browser or scan tabs to connect.",
        ["Chưa kết nối"] = "Not connected",
        ["Đã chọn: {0} | {1}"] = "Selected: {0} | {1}",

        ["Chạy thao tác: "] = "Run: ",
        ["Đang khởi động..."] = "Starting...",
        ["Đang chạy"] = "Running",
        ["Đã dừng"] = "Stopped",
        ["Đang chờ từ khóa"] = "Waiting for keyword",
        ["Đang tắt..."] = "Stopping...",
        ["Đã tắt"] = "Off",
        ["Đã click: {0}  |  Thành công: {1}  |  Lỗi: {2}  |  Thời gian: {3}"] =
            "Clicked: {0}  |  Success: {1}  |  Failed: {2}  |  Elapsed: {3}",

        ["Mở cửa sổ"] = "Open window",
        ["Thoát"] = "Exit",
        ["Ứng dụng vẫn chạy dưới khay hệ thống."] = "The app keeps running in the system tray.",
        ["Chạy thao tác đã dừng"] = "Run stopped",
        ["Click khi có từ khóa đã dừng"] = "Keyword watcher stopped",
        ["Đã dừng do lỗi"] = "Stopped due to an error",

        ["Sẵn sàng. Bấm \"Mở trình duyệt\" để mở trình duyệt, hoặc \"Quét tab\" nếu trình duyệt đã mở sẵn với cổng kết nối."] =
            "Ready. Click \"Open browser\" to start the browser, or \"Scan tabs\" if it is already running with the control port.",
        ["Thao tác trình duyệt gặp lỗi: {0}"] = "Browser operation failed: {0}",
        ["Đang kết nối trình duyệt để quét tab..."] = "Connecting to the browser to scan tabs...",
        ["Đang mở..."] = "Opening...",
        ["Không mở được trình duyệt: {0}"] = "Could not open the browser: {0}",
        ["Hãy chọn trình duyệt cần mở trong ô 'Chọn trình duyệt'."] = "Pick the browser to open in the 'Browser to open' list.",
        ["Trình duyệt đã mở sẵn với cổng kết nối. Hãy bấm 'Quét tab' để chọn tab."] =
            "The browser is already running with the control port. Click 'Scan tabs' to pick a tab.",
        ["Chưa chọn đường dẫn trình duyệt nên đã hủy."] = "No browser path selected, cancelled.",
        ["Không tìm thấy trình duyệt đã chọn. Hãy bấm nút '...' để chọn đường dẫn."] =
            "The selected browser was not found. Click '...' to pick its path.",
        ["Trình duyệt đang chạy nhưng chưa mở cổng kết nối nên không thể điều khiển."] =
            "The browser is running without the control port, so it cannot be controlled.",
        ["Trình duyệt đang mở mà không có cổng kết nối.\n\nĐóng trình duyệt để ứng dụng mở lại với cấu hình kết nối? Các tab chưa lưu có thể bị mất."] =
            "The browser is open without the control port.\n\nClose it so the app can reopen it with the control port? Unsaved tabs may be lost.",
        ["Đã hủy. Hãy tự đóng trình duyệt rồi bấm 'Mở trình duyệt' lại."] =
            "Cancelled. Close the browser yourself, then click 'Open browser' again.",
        ["Đang đóng trình duyệt..."] = "Closing the browser...",
        ["Trình duyệt vẫn đang chạy (có thể chạy nền). Hãy thoát hẳn rồi bấm 'Mở trình duyệt' lại."] =
            "The browser is still running (possibly in the background). Exit it fully, then click 'Open browser' again.",
        ["Đã đóng trình duyệt."] = "Browser closed.",
        ["Đang khởi chạy trình duyệt..."] = "Starting the browser...",
        ["Không khởi chạy được trình duyệt. Hãy kiểm tra đường dẫn: {0}"] =
            "Could not start the browser. Check the path: {0}",
        ["Trình duyệt đã mở thành công. Hãy bấm 'Quét tab' để chọn tab."] =
            "The browser started successfully. Click 'Scan tabs' to pick a tab.",
        ["Đã gọi lệnh mở nhưng cổng kết nối chưa sẵn sàng. Có thể trình duyệt còn chạy nền từ phiên trước, hoặc phiên bản trình duyệt không cho phép cổng gỡ lỗi với hồ sơ mặc định."] =
            "The browser was launched but the control port is not ready. It may still be running in the background, or this browser version does not allow the debug port with the default profile.",
        ["Đang kết nối lại..."] = "Reconnecting...",
        ["Mất kết nối trình duyệt. Đang thử kết nối lại ({0}/3)..."] = "Browser connection lost. Reconnecting ({0}/3)...",
        ["Chưa chọn được tab sau khi kết nối lại."] = "Could not select a tab after reconnecting.",
        ["Kết nối lại thất bại: {0}"] = "Reconnect failed: {0}",
        ["Trình duyệt đã đóng. Đang mở lại..."] = "The browser was closed. Reopening...",
        ["Cổng kết nối chưa sẵn sàng sau 15 giây."] = "The control port is not ready after 15 seconds.",
        ["Không mở lại được trình duyệt: {0}"] = "Could not reopen the browser: {0}",
        ["Không thấy tab cũ. Đang mở lại: {0}"] = "The previous tab was not found. Reopening: {0}",
        ["Đã kết nối lại và chọn tab: {0}"] = "Reconnected and selected tab: {0}",
        ["Không đăng ký được phím tắt toàn cục (có thể ứng dụng khác đang dùng F6/F7/F8)."] =
            "Could not register global hotkeys (another app may be using F6/F7/F8).",
        ["Trình duyệt sẽ mở: {0}"] = "Browser to open: {0}",
        ["Chương trình (*.exe)|*.exe"] = "Programs (*.exe)|*.exe",
        ["Không lưu được đường dẫn trình duyệt: {0}"] = "Could not save the browser path: {0}",
        ["Đã chọn trình duyệt: {0}"] = "Browser selected: {0}",
        ["Đã xuất nhật ký: {0}"] = "Log exported: {0}",
        ["Không xuất được nhật ký: {0}"] = "Could not export the log: {0}",
        ["Tìm thấy {0} tab; đang chọn: {1}"] = "Found {0} tab(s); selected: {1}",
        ["Tìm thấy {0} tab. Hãy chọn tab cần dùng trong danh sách."] = "Found {0} tab(s). Pick one from the list.",
        ["Không tìm thấy tab đang mở."] = "No open tab was found.",
        ["Đã chọn tab: {0} | {1}"] = "Tab selected: {0} | {1}",
        ["Không thể chọn tab: {0}"] = "Could not select the tab: {0}",

        ["Vui lòng nhập Selector trước."] = "Enter a selector first.",
        ["Hãy hoàn tất hoặc hủy thao tác chọn phần tử trước."] = "Finish or cancel the element picking first.",
        ["Hãy kết nối và chọn tab trước."] = "Connect and select a tab first.",
        ["✗ Selector không hợp lệ: {0}"] = "✗ Invalid selector: {0}",
        ["Selector hợp lệ nhưng chưa tìm thấy phần tử nào."] = "The selector is valid but no element was found.",
        ["✓ Selector hợp lệ  |  Tìm thấy: {0}  |  Hiển thị: {1}"] =
            "✓ Valid selector  |  Matches: {0}  |  Visible: {1}",
        ["Không thể kiểm tra Selector: {0}"] = "Could not validate the selector: {0}",
        ["Hãy quét tab và chọn một tab trước."] = "Scan tabs and select one first.",
        ["Không thấy văn bản nào đang hiển thị trên màn hình."] = "No text is visible on screen.",
        ["Văn bản đang hiển thị trên màn hình ({0}; ★ = khớp từ khóa):"] =
            "Text visible on screen ({0}; ★ = matches a keyword):",
        ["Không thể đọc văn bản trên trang: {0}"] = "Could not read the page text: {0}",
        ["Hãy kết nối và chọn tab trước khi chọn phần tử."] = "Connect and select a tab before picking an element.",
        ["Đang có thao tác khác. Hãy dừng các chức năng đang chạy trước khi chọn phần tử."] =
            "Another operation is running. Stop it before picking an element.",
        ["Đang có thao tác khác. Vui lòng thử lại."] = "Another operation is running. Try again.",
        ["Đang chọn..."] = "Picking...",
        ["Đang chờ bạn chọn phần tử trên trình duyệt (Esc để hủy)."] = "Waiting for you to click an element in the browser (Esc to cancel).",
        ["Đang chuyển sang trình duyệt. Hãy bấm vào phần tử cần chọn (Esc để hủy); ứng dụng sẽ tự quay lại sau khi bạn chọn."] =
            "Switching to the browser. Click the element to pick (Esc to cancel); the app returns automatically afterwards.",
        ["Không tự chuyển được sang trình duyệt. Hãy tự mở tab đó rồi bấm vào phần tử."] =
            "Could not switch to the browser automatically. Open that tab yourself, then click the element.",
        ["Đã hủy chọn phần tử."] = "Element picking cancelled.",
        ["Đã tạo Selector. Hãy bấm Kiểm tra trước khi lưu hoặc chạy."] = "Selector created. Click Validate before saving or running.",
        ["Đã chọn phần tử và tạo Selector: {0}"] = "Element picked, selector created: {0}",
        ["Không thể chọn phần tử: {0}"] = "Could not pick the element: {0}",
        ["Vui lòng nhập tên cho Selector."] = "Enter a name for the selector.",
        ["Vui lòng nhập CSS Selector."] = "Enter a CSS selector.",
        ["Đã lưu Selector \"{0}\"."] = "Selector \"{0}\" saved.",
        ["Đã xóa Selector \"{0}\"."] = "Selector \"{0}\" deleted.",
        ["Không thể tải danh sách Selector đã lưu: {0}"] = "Could not load saved selectors: {0}",
        ["Không thể lưu Selector: {0}"] = "Could not save the selector: {0}",
        ["Vui lòng kết nối và chọn tab trước."] = "Connect and select a tab first.",
        ["Đã làm mới trang web."] = "Page reloaded.",
        ["Không thể làm mới trang web: {0}"] = "Could not reload the page: {0}",

        ["Chạy thao tác đang chạy."] = "The run is already active.",
        ["Vui lòng nhập CSS Selector trước khi bắt đầu."] = "Enter a CSS selector before starting.",
        ["Không thể bắt đầu: {0}"] = "Could not start: {0}",
        ["Selector không hợp lệ. Vui lòng kiểm tra lại."] = "The selector is invalid. Check it again.",
        ["Chưa thấy phần tử. Sẽ chờ tối đa {0} giây."] = "No element yet. Will wait up to {0} seconds.",
        ["Lưu ý: 'Click khi có từ khóa' đang dùng cùng Selector này. Hai chức năng sẽ cùng bấm vào một nút."] =
            "Note: 'Click on keyword' uses the same selector. Both features will click the same button.",
        ["Chạy thao tác: bắt đầu bấm Selector theo chu kỳ. Bấm Dừng để kết thúc."] =
            "Run: clicking the selector repeatedly. Click Stop to finish.",
        ["Không thấy Selector hiển thị trong thời gian chờ."] = "The selector did not appear within the wait time.",
        ["Chạy thao tác đã dừng do lỗi: {0}"] = "Run stopped due to an error: {0}",
        ["Chạy thao tác đã dừng."] = "Run stopped.",
        ["Đang dừng chạy thao tác..."] = "Stopping the run...",
        ["Đang có thao tác khác. Vui lòng chờ thao tác hiện tại hoàn tất."] =
            "Another operation is running. Wait for it to finish.",
        ["Vui lòng nhập Selector trước khi Click."] = "Enter a selector before clicking.",
        ["Đang có click khác."] = "Another click is in progress.",
        ["Đang chờ Selector tối đa {0} giây..."] = "Waiting for the selector up to {0} seconds...",
        ["Click thành công."] = "Clicked successfully.",
        ["Đã hủy thao tác Click."] = "Click cancelled.",
        ["Không tìm thấy phần tử trong thời gian chờ. Hãy kiểm tra Selector và thử lại."] =
            "Element not found within the wait time. Check the selector and try again.",
        ["Click lỗi: {0}"] = "Click failed: {0}",
        ["Click khi có từ khóa đang tắt. Hãy thử bật lại sau giây lát."] =
            "Keyword watcher is turning off. Try enabling it again in a moment.",
        ["Đang có thao tác khác. Hãy thử lại sau."] = "Another operation is running. Try again later.",
        ["Hãy mở trình duyệt, quét tab và chọn một tab trước."] = "Open the browser, scan tabs and select one first.",
        ["Vui lòng nhập Selector (nút 'Video tiếp theo') trước khi bật chức năng này."] =
            "Enter the selector (the 'Next video' button) before enabling this feature.",
        ["Vui lòng nhập ít nhất một từ khóa (cách nhau bằng dấu phẩy)."] =
            "Enter at least one keyword (separated by commas).",
        ["Click khi có từ khóa: đã bật. Từ khóa: {0}."] = "Keyword watcher enabled. Keywords: {0}.",
        ["Click khi có từ khóa đã dừng do lỗi: {0}"] = "Keyword watcher stopped due to an error: {0}",
        ["Click khi có từ khóa: đã tắt."] = "Keyword watcher disabled.",
        ["Đã chuyển sang giao diện tối."] = "Switched to dark mode.",
        ["Đã chuyển sang giao diện sáng."] = "Switched to light mode.",
        ["Một tác vụ nền kết thúc khi đóng ứng dụng: {0}"] = "A background task ended while closing: {0}",
        ["Không thể đóng hoàn toàn kết nối trình duyệt: {0}"] = "Could not fully close the browser connection: {0}",

        ["Đã click Selector."] = "Clicked the selector.",
        ["Không thấy Selector hiển thị sau {0} giây chờ. Dừng chạy thao tác để tránh lặp lại."] =
            "The selector did not appear after {0} seconds. Stopping the run to avoid repeats.",
        ["Click lỗi ({0}/{1}): {2}"] = "Click failed ({0}/{1}): {2}",
        ["Bỏ qua click trùng: phần tử chưa thay đổi trạng thái."] = "Skipped duplicate click: the element is unchanged.",
        ["Mất kết nối trình duyệt và không thể kết nối lại."] = "Lost the browser connection and could not reconnect.",
        ["Mất kết nối trình duyệt."] = "Lost the browser connection.",
        ["Đã kết nối lại. Tiếp tục chạy thao tác."] = "Reconnected. Continuing the run.",

        ["Đang chờ từ khóa... (đã xử lý {0} lần)"] = "Waiting for a keyword... ({0} handled)",
        ["Đã chuyển video liên tiếp {0} lần nhưng vẫn thấy \"{1}\" ({2}). Có thể từ khóa trùng với chữ cố định trên trang, hoặc nút 'Video tiếp theo' không hoạt động. Đã dừng chức năng này."] =
            "Advanced the video {0} times in a row but \"{1}\" is still visible ({2}). The keyword may be static page text, or the 'Next video' button is not working. Feature stopped.",
        ["Phát hiện từ khóa \"{0}\" trong \"{1}\". Đang chuyển video..."] =
            "Keyword \"{0}\" found in \"{1}\". Advancing the video...",
        ["Đã chuyển video bằng {0}."] = "Advanced the video using {0}.",
        ["Lỗi ({0}/{1}): {2}"] = "Error ({0}/{1}): {2}",
        ["Tab hoặc trình duyệt đã đóng và không thể kết nối lại. Hãy quét tab và chọn lại."] =
            "The tab or browser closed and could not be reconnected. Scan tabs and select again.",
        ["Đã kết nối lại. Tiếp tục theo dõi từ khóa."] = "Reconnected. Continuing to watch keywords.",
        ["nút Selector"] = "the selector button",
        ["phím ↓ (không thấy nút Selector)"] = "the down key (selector button not found)",

        ["Không có thông tin chi tiết."] = "No details available.",
        ["Không kết nối được với trình duyệt. Hãy bấm 'Mở trình duyệt' (hoặc mở trình duyệt với cổng {0}) rồi quét lại. Chi tiết: {1}"] =
            "Could not connect to the browser. Click 'Open browser' (or start it with port {0}) and scan again. Details: {1}",
        ["Lỗi trình duyệt: {0}"] = "Browser error: {0}",
        ["Ứng dụng gặp lỗi không mong đợi:\n\n{0}\n\nNếu lỗi lặp lại, hãy gửi nội dung này cho nhà phát triển."] =
            "The app hit an unexpected error:\n\n{0}\n\nIf it keeps happening, send this to the developer.",

        ["Không tìm thấy Browser Context."] = "Browser context not found.",
        ["Chưa kết nối trình duyệt."] = "The browser is not connected.",
        ["(Không lấy được tiêu đề)"] = "(Title unavailable)",
        ["(Không có tiêu đề)"] = "(No title)",
        ["Tab đã đóng. Hãy quét lại danh sách tab."] = "The tab is closed. Scan the tab list again.",
        ["CSS Selector không được để trống."] = "The CSS selector cannot be empty.",
        ["Selector đang trống."] = "The selector is empty.",
        ["Chưa chọn tab trình duyệt."] = "No browser tab selected.",
        ["Hệ điều hành không khởi chạy được tiến trình trình duyệt."] = "The OS could not start the browser process.",
        ["Không tìm thấy phần tử hiển thị trong {0} giây."] = "The element did not appear within {0} seconds.",
        ["Bấm vào phần tử cần chọn — Esc để hủy"] = "Click the element to pick — Esc to cancel"
    };

    public static string Language { get; private set; } = Vietnamese;

    public static bool IsEnglish => Language == English;

    public static void SetLanguage(string language)
    {
        string next = string.Equals(language, English, StringComparison.OrdinalIgnoreCase)
            ? English
            : Vietnamese;

        Language = next;
    }

    public static string T(string vietnamese)
    {
        if (!IsEnglish)
            return vietnamese;

        return Translations.TryGetValue(vietnamese, out string? english) ? english : vietnamese;
    }

    public static string F(string vietnamese, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, T(vietnamese), args);

    public static void Apply(Control root)
    {
        if (root.Tag is string original)
            root.Text = T(original);

        foreach (Control child in root.Controls)
            Apply(child);
    }
}
