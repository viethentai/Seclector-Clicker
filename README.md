# Auto Click Selector

Ứng dụng Windows (WinForms, .NET 10) giúp tự động bấm một phần tử (CSS Selector) trên
trình duyệt Chromium (CốC CốC, Microsoft Edge, Google Chrome) theo chu kỳ, hoặc tự chuyển
video khi thấy từ khóa quảng cáo. Ứng dụng điều khiển trình duyệt qua cổng remote debugging
(mặc định `9222`) bằng Playwright.

## Tính năng

- **Chọn trình duyệt**: chọn CốC CốC, Microsoft Edge, Google Chrome hoặc trỏ tới tệp `.exe` bất kỳ.
- **Trình duyệt**: mở trình duyệt đã chọn với cổng điều khiển, quét và chọn tab đang dùng.
- **Selector**: chọn phần tử trực tiếp trên trang, kiểm tra Selector, lưu/xóa Selector theo tên.
- **Chạy thao tác**: khoảng nghỉ, thời gian chờ, bắt đầu/dừng, click một lần, làm mới trang,
  thống kê số lần click, thành công, lỗi và thời gian chạy.
- **Click khi có từ khóa**: theo dõi chữ đang hiển thị trong khung nhìn; khi thấy từ khóa thì chuyển video.
- **Chống click trùng**: bỏ qua lần bấm nếu phần tử vẫn giữ nguyên trạng thái như lần trước.
- **Tự phục hồi**: mất kết nối thì tự kết nối lại, mở lại trình duyệt nếu đã tắt, chọn lại đúng tab
  theo URL gần nhất (hoặc mở lại URL đó nếu không còn tab).
- **Nhật ký**: lọc theo mức, tìm kiếm, xuất ra tệp `.txt`.
- **Khay hệ thống**: thu nhỏ/nút X ẩn xuống khay, menu Bắt đầu/Dừng/Thoát, thông báo khi dừng hoặc lỗi.
- **Phím tắt toàn cục**: F6/F7/F8 dùng được cả khi cửa sổ không có focus.
- **Giao diện sáng/tối** và **song ngữ Việt/Anh**, ghi nhớ lựa chọn giữa các lần mở.

## Cấu trúc mã nguồn

```
SeclectorClicker/
├── Core/
│   ├── BrowserController.cs         Playwright: quét tab, mở tab, bấm, nhập chữ, đọc chữ
│   ├── BrowserLauncher.cs           Tìm, khởi chạy, đóng trình duyệt theo từng hãng
│   ├── GlobalHotkey.cs              Đăng ký F6/F7/F8 toàn cục
│   ├── SelectorRepository.cs        Đọc/ghi danh sách Selector đã lưu
│   ├── SettingsStore.cs             Đọc/ghi app-settings.json
│   ├── WindowActivator.cs           Win32: đưa cửa sổ trình duyệt lên trước
│   ├── WindowsTheme.cs              Đọc chế độ sáng/tối mặc định của Windows
│   └── SelectorWaitTimeoutException.cs
├── Models/
│   ├── AppSettings.cs               Toàn bộ cấu hình được lưu
│   ├── BrowserTab.cs                Một tab trong trình duyệt
│   ├── SelectorValidationResult.cs  Kết quả kiểm tra Selector
│   ├── ActivityLevel.cs             Mức độ dòng nhật ký
│   └── KeywordState.cs              Trạng thái chức năng từ khóa
├── Services/
│   ├── AutoClickService.cs          Vòng lặp bấm Selector theo chu kỳ
│   ├── KeywordWatcherService.cs     Vòng lặp theo dõi từ khóa và chuyển video
│   ├── Localization.cs              Bản dịch Việt → Anh cho giao diện
│   ├── NotificationService.cs       Thông báo qua khay hệ thống
│   └── TextFormatter.cs             Định dạng URL, từ khóa, thông báo lỗi
├── UI/
│   ├── Forms/
│   │   ├── MainForm.cs              Logic và điều phối sự kiện
│   │   └── MainForm.Layout.cs       Dựng bố cục và control
│   ├── Controls/                    Control tùy chỉnh (tự vẽ theo bảng màu)
│   ├── Components/                  Thẻ nhóm, điều hướng con lăn
│   └── Themes/                      Màu sắc, font, theme
├── Resources/
│   └── icon.ico
├── Program.cs
├── Auto Click Selector.csproj
└── README.md
```

`Core` không phụ thuộc giao diện; `Services` nhận callback (log, ghi nhận click) thay vì
tham chiếu trực tiếp form; mọi chuỗi hiển thị dùng tiếng Việt có dấu và có bản dịch tiếng Anh.

## Yêu cầu

- Windows 10 (1809 trở lên) hoặc Windows 11.
- .NET 10 SDK, có Windows Desktop workload.
- Một trình duyệt Chromium: CốC CốC, Microsoft Edge hoặc Google Chrome.

## Build và chạy

```
dotnet restore
dotnet build -c Release
dotnet run
```

Lần đầu build sẽ tải gói `Microsoft.Playwright` từ NuGet.

Kiểm tra nhanh không mở cửa sổ:

```
"Auto Click Selector.exe" --selftest
```

Mã thoát `0` là khởi tạo và bố cục thành công; khác `0` là có lỗi, chi tiết ghi trong
`selftest-step.txt` cạnh tệp thực thi.

## Hướng dẫn sử dụng nhanh

1. Chọn trình duyệt trong ô **Chọn trình duyệt** (nút `...` để trỏ tới tệp `.exe` khác).
2. Bấm **Mở trình duyệt** (hoặc mở sẵn trình duyệt với cổng `9222` rồi bấm **Quét tab**).
3. Chọn tab cần điều khiển trong danh sách **Tab đang điều khiển**.
4. Bấm **Chọn phần tử**, bấm vào phần tử trên trang, rồi bấm **Kiểm tra**.
5. Đặt khoảng nghỉ và thời gian chờ, bấm **Bắt đầu** (F6). Bấm **Dừng** (F7) để kết thúc.
6. Để tự chuyển video khi gặp quảng cáo: nhập từ khóa và bật **Bật theo dõi từ khóa**.
   Nút **Click 1 lần** (F8) vẫn dùng được trong khi theo dõi từ khóa.

## Phím tắt

| Phím | Tác dụng |
|------|----------|
| F6 | Bắt đầu chạy thao tác |
| F7 | Dừng chạy thao tác |
| F8 | Click một lần |

Có thể tắt phím tắt toàn cục trong **Cài đặt** nếu bị trùng với ứng dụng khác.

## Dữ liệu được lưu

Tất cả nằm trong `%LOCALAPPDATA%\AutoClickSelector\`:

| File                    | Nội dung                                              |
|-------------------------|-------------------------------------------------------|
| `app-settings.json`     | Cấu hình giao diện, chu kỳ, từ khóa, chống click trùng |
| `selectors.json`        | Danh sách Selector đã lưu theo tên                    |
| `browser-settings.json` | Đường dẫn `.exe` của trình duyệt tự chọn              |

Bản cũ dùng `ui-settings.json`; lần chạy đầu tiên sẽ tự đọc tệp đó nếu chưa có `app-settings.json`.

## Lưu ý an toàn

- Ứng dụng chỉ kết nối với cổng debug trên `127.0.0.1`. Không mở cổng này ra mạng.
- Khi đang bật cổng debug, bất kỳ chương trình nào trên máy đều có thể điều khiển trình duyệt.
  Chỉ bật khi cần và tắt sau khi dùng.
- Nhật ký chỉ ghi domain và đường dẫn trang, không ghi query string, để tránh lộ token hay tham số phiên.
- Một số bản Chromium mới có thể từ chối bật cổng gỡ lỗi với hồ sơ mặc định; khi đó nhật ký sẽ báo
  cổng kết nối không sẵn sàng.

## Ghi chú về giao diện

- Các control nhập liệu, danh sách, ô số và thanh cuộn đều tự vẽ, lấy màu từ `UiTheme`
  và được vẽ lại khi đổi chế độ sáng/tối.
- Cửa sổ có kích thước cố định (1180 × 800): không kéo giãn và không phóng to, vẫn có nút thu nhỏ.
- Chế độ màu trang web: khi chọn tab, ứng dụng đặt `prefers-color-scheme` của trang theo chế độ mặc định của Windows.
