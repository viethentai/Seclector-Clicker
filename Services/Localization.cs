namespace AutoClickSelector.Services;

public static class Localization
{
    public const string Vietnamese = "vi";
    public const string English = "en";

    private static readonly Dictionary<string, string> Translations = new(StringComparer.Ordinal)
    {
        ["Tự động thao tác trên trình duyệt"] = "Automate actions in your browser",
        ["Chế độ tối"] = "Dark mode",
        ["Ngôn ngữ"] = "Language",

        ["1. Trình duyệt"] = "1. Browser",
        ["Mở trình duyệt"] = "Open browser",
        ["Quét tab"] = "Scan tabs",
        ["Tab đang điều khiển"] = "Controlled tab",
        ["Chọn trình duyệt cần mở"] = "Browser to open",
        ["Chọn trình duyệt"] = "Choose browser",
        ["Trình duyệt khác"] = "Other browser",

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

        ["Thu nhỏ xuống khay khi chạy"] = "Minimize to tray",
        ["Thoát xuống khay khi bấm X"] = "Close button hides to tray",
        ["Thông báo khi dừng hoặc lỗi"] = "Notify on stop or error",
        ["Phím tắt toàn cục (F6/F7/F8)"] = "Global hotkeys (F6/F7/F8)",
        ["Bỏ qua click trùng khi phần tử không đổi"] = "Skip duplicate clicks when the element is unchanged",
        ["5. Cài đặt"] = "5. Settings",

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

        ["Chạy thao tác: "] = "Run: ",
        ["Đang khởi động..."] = "Starting...",
        ["Đang chạy"] = "Running",
        ["Đã dừng"] = "Stopped",
        ["Đang chờ từ khóa"] = "Waiting for keyword",
        ["Đang xử lý"] = "Processing",
        ["Đang tắt..."] = "Stopping...",
        ["Đã tắt"] = "Off",
        ["Đã click: {0}  |  Thành công: {1}  |  Lỗi: {2}  |  Thời gian: {3}"] =
            "Clicked: {0}  |  Success: {1}  |  Failed: {2}  |  Elapsed: {3}",

        ["Mở cửa sổ"] = "Open window",
        ["Thoát"] = "Exit",
        ["Ứng dụng vẫn chạy dưới khay hệ thống."] = "The app keeps running in the system tray.",
        ["Chạy thao tác đã dừng"] = "Run stopped",
        ["Click khi có từ khóa đã dừng"] = "Keyword watcher stopped",
        ["Đã dừng do lỗi"] = "Stopped due to an error"
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

    public static void Apply(Control root)
    {
        if (root.Tag is string original)
            root.Text = T(original);

        foreach (Control child in root.Controls)
            Apply(child);
    }
}
