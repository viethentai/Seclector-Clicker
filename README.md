# Auto Click Selector

Ứng dụng Windows (WinForms, .NET 10) giúp tự động bấm một phần tử (CSS Selector) trên
trình duyệt CốC CốC theo chu kỳ, hoặc tự chuyển video khi thấy từ khóa quảng cáo.
Ứng dụng điều khiển CốC CốC qua cổng remote debugging (mặc định `9222`) bằng Playwright.

## Tính năng

- **Trình duyệt**: mở CốC CốC với cổng điều khiển, quét và chọn tab đang dùng.
- **Selector**: chọn phần tử trực tiếp trên trang, kiểm tra Selector, lưu/xóa Selector theo tên.
- **Chu kỳ click**: đặt khoảng nghỉ (giây/phút) và thời gian chờ tối đa để Selector hiển thị.
- **Chạy thao tác**: bấm Selector lặp lại theo chu kỳ; có thống kê số lần click, thành công, lỗi.
- **Click khi có từ khóa**: theo dõi chữ đang hiển thị trong khung nhìn; khi thấy từ khóa thì chuyển video.
- **Giao diện sáng/tối**: chuyển mượt, ghi nhớ lựa chọn giữa các lần mở.
- **Phím tắt**: F6 (bắt đầu), F7 (dừng), F8 (click một lần).

## Cấu trúc mã nguồn

```
SeclectorClicker/
├── Core/                  Xử lý nghiệp vụ
│   ├── BrowserController.cs         Playwright: quét tab, bấm, đọc chữ, chọn phần tử
│   ├── BrowserLauncher.cs           Tìm, khởi chạy, đóng CốC CốC
│   ├── SelectorRepository.cs        Đọc/ghi danh sách Selector đã lưu (không phụ thuộc WinForms)
│   ├── WindowActivator.cs           Win32: đưa cửa sổ trình duyệt lên trước
│   ├── WindowsTheme.cs              Đọc chế độ sáng/tối mặc định của Windows
│   └── SelectorWaitTimeoutException.cs
├── Models/                Dữ liệu
│   ├── AppPreferences.cs            Tùy chọn giao diện
│   ├── BrowserTab.cs                Một tab trong trình duyệt
│   ├── SelectorValidationResult.cs  Kết quả kiểm tra Selector
│   ├── ActivityLevel.cs             Mức độ dòng nhật ký
│   └── KeywordState.cs              Trạng thái chức năng từ khóa
├── Services/              Dịch vụ ứng dụng dùng chung
│   ├── AutoClickService.cs          Vòng lặp bấm Selector theo chu kỳ
│   ├── KeywordWatcherService.cs     Vòng lặp theo dõi từ khóa và chuyển video
│   └── TextFormatter.cs             Định dạng URL, từ khóa, thông báo lỗi
├── UI/
│   ├── Forms/
│   │   ├── MainForm.cs              Logic và điều phối sự kiện
│   │   └── MainForm.Layout.cs       Dựng bố cục và control
│   ├── Controls/                    Control tùy chỉnh (tự vẽ theo bảng màu)
│   │   ├── ThemedComboBox.cs        Ô chọn bo góc, danh sách thả xuống tự vẽ
│   │   ├── ThemedNumericBox.cs      Ô số, nút tăng/giảm tự vẽ
│   │   ├── ThemedScrollBar.cs       Thanh cuộn tự vẽ
│   │   ├── RoundedTextBox.cs        Ô nhập bo góc
│   │   ├── ThemedButton.cs          Nút bo góc, nhiều kiểu (Primary, Danger, Ghost...)
│   │   ├── ToggleSwitch.cs          Công tắc bật/tắt
│   │   └── LogView.cs               Nhật ký có màu theo mức độ
│   ├── Components/
│   │   ├── SectionCard.cs           Thẻ có tiêu đề, nền bo góc
│   │   └── WheelRouter.cs           Chuyển bánh xe chuột tới vùng cuộn tự vẽ
│   └── Themes/                      Màu sắc, font, theme
│       ├── Palette.cs               Bảng màu sáng/tối và phép nội suy
│       ├── UiTheme.cs               Theme hiện tại, font dùng chung
│       └── UiDrawing.cs             Hàm vẽ hình chữ nhật bo góc
├── Resources/
│   └── icon.ico
├── Program.cs
├── Auto Click Selector.csproj
└── README.md
```

Quy ước: `Core` không phụ thuộc giao diện; `Services` nhận các hàm callback (log, ghi nhận click)
thay vì tham chiếu trực tiếp form; mọi chuỗi hiển thị dùng tiếng Việt có dấu.

## Yêu cầu

- Windows 10 (1809 trở lên) hoặc Windows 11.
- .NET 10 SDK, có Windows Desktop workload.
- CốC CốC đã cài đặt (đường dẫn mặc định `%LOCALAPPDATA%\CocCoc\Browser\Application\browser.exe`
  hoặc trong `Program Files`; nếu không tìm thấy, ứng dụng sẽ hỏi).

## Build và chạy

1. Mở `Auto Click Selector.csproj` bằng Visual Studio có hỗ trợ .NET 10 (kèm workload ".NET desktop development") hoặc dùng dòng lệnh:

   ```
   dotnet restore
   dotnet build -c Release
   dotnet run
   ```

2. Lần đầu build sẽ tải gói `Microsoft.Playwright` từ NuGet.

## Hướng dẫn sử dụng nhanh

1. Bấm **Mở trình duyệt** (hoặc mở CốC CốC sẵn với cổng `9222` rồi bấm **Quét tab**). Chọn tab cần điều khiển.
2. Bấm **Chọn phần tử**, rồi bấm vào phần tử trên trang. Bấm **Kiểm tra** để xem số phần tử khớp.
3. Đặt chu kỳ click và thời gian chờ, sau đó bấm **Bắt đầu** (F6). Bấm **Dừng** (F7) để kết thúc.
4. Để tự chuyển video khi gặp quảng cáo: nhập từ khóa, bật công tắc **Bật theo dõi từ khóa**.
   Có thể bấm **Xem chữ hiển thị** để xem những chữ đang nằm trên màn hình và chọn từ khóa phù hợp.

Lưu ý: Nếu CốC CốC đang mở mà không có cổng `9222`, ứng dụng sẽ hỏi có đóng CốC CốC để mở lại không.
Hãy lưu công việc trước khi đồng ý.

## Dữ liệu được lưu

Tất cả nằm trong `%LOCALAPPDATA%\AutoClickSelector\`:

| File                  | Nội dung                              |
|-----------------------|---------------------------------------|
| `selectors.json`      | Danh sách Selector đã lưu theo tên    |
| `browser-settings.json` | Đường dẫn `browser.exe` đã chọn     |
| `ui-settings.json`    | Chế độ sáng/tối                       |

## Lưu ý an toàn

- Ứng dụng chỉ kết nối với cổng debug trên `127.0.0.1`. Không mở cổng này ra mạng.
- Khi đang bật cổng debug, bất kỳ chương trình nào trên máy đều có thể điều khiển CốC CốC.
  Chỉ bật khi cần và tắt sau khi dùng.
- Nhật ký chỉ ghi domain và đường dẫn trang, không ghi query string, để tránh lộ token hay tham số phiên.

## Ghi chú về giao diện

- Các control nhập liệu, danh sách, ô số và thanh cuộn đều tự vẽ, lấy màu từ `UiTheme`
  và được vẽ lại khi đổi chế độ sáng/tối.
- Cửa sổ có kích thước cố định (1180 × 800): không kéo giãn và không phóng to, vẫn có nút thu nhỏ.
- Chế độ màu trang web: khi chọn tab, ứng dụng đặt `prefers-color-scheme` của trang theo chế độ mặc định của Windows
  (các trang tự chọn sáng/tối bằng nút trên web không bị ảnh hưởng).
