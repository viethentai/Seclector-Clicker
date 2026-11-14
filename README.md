# Auto Click Selector

Ứng dụng Windows (WinForms, .NET 10) giúp tự động bấm một phần tử (CSS Selector) trên
trình duyệt Chromium (Cốc Cốc, Microsoft Edge, Google Chrome hoặc trình duyệt khác chọn thủ công)
theo chu kỳ, hoặc tự chuyển video khi thấy từ khóa quảng cáo.

Ứng dụng điều khiển trình duyệt qua cổng remote debugging (mặc định `9222`) bằng Playwright
(kết nối CDP vào trình duyệt đã cài sẵn, không cần tải trình duyệt riêng của Playwright).

## Tính năng

- **Chọn trình duyệt**: Cốc Cốc, Microsoft Edge, Google Chrome hoặc **Trình duyệt khác** (trỏ tới tệp
  `.exe` bất kỳ, ví dụ Opera). Đường dẫn tự chọn được ghi nhớ.
- **Nhận diện trình duyệt trên cổng 9222**: ứng dụng kiểm tra trình duyệt đang giữ cổng có khớp lựa chọn
  hay không, để tránh điều khiển nhầm trình duyệt khác. Với **Trình duyệt khác** bước này được bỏ qua.
- **Trình duyệt**: mở trình duyệt đã chọn với cổng điều khiển, quét và chọn tab đang dùng.
- **Selector**: chọn phần tử trực tiếp trên trang (có đánh dấu khi rê chuột, Esc để hủy), kiểm tra
  Selector (số phần tử tìm thấy / đang hiển thị), lưu và xóa Selector theo tên.
- **Chạy thao tác**: khoảng nghỉ, thời gian chờ Selector hiển thị, bắt đầu/dừng, click một lần, làm mới
  trang, thống kê số lần click, thành công, lỗi và thời gian chạy.
- **Click khi có từ khóa**: theo dõi chữ đang hiển thị trong khung nhìn (kể cả trong iframe và shadow DOM);
  khi thấy từ khóa thì bấm Selector để chuyển video, nếu không thấy nút thì dùng phím mũi tên xuống.
  Từ khóa không phân biệt hoa/thường và dấu tiếng Việt.
- **Chống click trùng**: bỏ qua lần bấm nếu phần tử vẫn giữ nguyên trạng thái như lần trước.
- **Tự phục hồi**: mất kết nối thì tự kết nối lại (tối đa 3 lần), mở lại trình duyệt nếu đã tắt, chọn lại
  đúng tab theo URL gần nhất (hoặc mở lại URL đó nếu không còn tab).
- **Nhật ký**: lọc theo mức, tìm kiếm, xuất ra tệp `.txt`, giữ tối đa 500 dòng gần nhất.
- **Khay hệ thống**: thu nhỏ/nút X ẩn xuống khay, menu Bắt đầu/Dừng/Thoát, thông báo khi dừng hoặc lỗi.
- **Phím tắt toàn cục**: F6/F7/F8 dùng được cả khi cửa sổ không có focus.
- **Giao diện sáng/tối** và **song ngữ Việt/Anh**, ghi nhớ lựa chọn giữa các lần mở.

### Khi nào ứng dụng tự dừng

- **Chạy thao tác** dừng khi không thấy Selector hiển thị trong thời gian chờ, hoặc khi gặp lỗi liên tiếp
  3 lần. Nếu chỉ mất kết nối trình duyệt, ứng dụng thử kết nối lại trước.
- **Click khi có từ khóa** tự tắt nếu đã chuyển video liên tiếp 5 lần mà vẫn thấy từ khóa (thường do từ khóa
  trùng với chữ cố định trên trang, hoặc nút chuyển video không hoạt động).

## Cấu trúc mã nguồn

```
Seclector-Clicker/
├── .github/workflows/
│   └── dotnet-build.yml             CI: build Release và chạy unit test trên Windows
├── Core/
│   ├── BrowserController.cs         Playwright: quét tab, mở tab, bấm, nhập chữ, đọc chữ
│   ├── BrowserLauncher.cs           Tìm, khởi chạy, đóng trình duyệt; nhận diện trình duyệt trên cổng debug
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
├── Properties/
│   └── AssemblyInfo.cs              Cho phép dự án test truy cập thành phần internal
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
├── tests/
│   └── AutoClickSelector.Tests/     Unit test (xUnit), hiện kiểm tra nhận diện trình duyệt
├── Program.cs
├── Auto Click Selector.csproj
└── README.md
```

`Core` không phụ thuộc giao diện; `Services` nhận callback (log, ghi nhận click) thay vì
tham chiếu trực tiếp form; mọi chuỗi hiển thị dùng tiếng Việt có dấu và có bản dịch tiếng Anh
trong `Localization.cs`.

## Yêu cầu

- Windows 10 (1809 trở lên) hoặc Windows 11.
- .NET 10 SDK, có Windows Desktop workload.
- Một trình duyệt Chromium: Cốc Cốc, Microsoft Edge, Google Chrome hoặc trình duyệt khác.

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

## Kiểm thử và CI

Chạy unit test (trên Windows):

```
dotnet test tests/AutoClickSelector.Tests/AutoClickSelector.Tests.csproj -c Release
```

Workflow `.github/workflows/dotnet-build.yml` build Release và chạy test trên `windows-latest` khi push
lên nhánh `update` hoặc `security-hardening`, khi mở pull request vào `main`, hoặc chạy tay.

## Hướng dẫn sử dụng nhanh

1. Chọn trình duyệt trong ô **Chọn trình duyệt** (nút `...` để trỏ tới tệp `.exe` khác).
2. Bấm **Mở trình duyệt** (hoặc mở sẵn trình duyệt với cổng `9222` rồi bấm **Quét tab**).
3. Chọn tab cần điều khiển trong danh sách **Tab đang điều khiển**.
4. Bấm **Chọn phần tử**, bấm vào phần tử trên trang, rồi bấm **Kiểm tra**.
5. Đặt khoảng nghỉ (tối thiểu 0,2 theo đơn vị đã chọn là giây hoặc phút) và thời gian chờ (1 đến 600 giây),
   bấm **Bắt đầu** (F6). Bấm **Dừng** (F7) để kết thúc.
6. Để tự chuyển video khi gặp quảng cáo: nhập từ khóa (cách nhau bằng dấu phẩy hoặc chấm phẩy) và bật
   **Bật theo dõi từ khóa**. Nút **Click 1 lần** (F8) vẫn dùng được trong khi theo dõi từ khóa.
   Nút **Xem chữ hiển thị** liệt kê chữ đang thấy trên trang (★ là khớp từ khóa) để chọn từ khóa cho đúng.

## Phím tắt

| Phím | Tác dụng |
|------|----------|
| F6 | Bắt đầu chạy thao tác |
| F7 | Dừng chạy thao tác |
| F8 | Click một lần |

Có thể tắt phím tắt toàn cục trong **Cài đặt** nếu bị trùng với ứng dụng khác. Khi tắt, F6/F7/F8
vẫn dùng được lúc cửa sổ ứng dụng đang có focus.

## Dữ liệu được lưu

Tất cả nằm trong `%LOCALAPPDATA%\AutoClickSelector\`:

| File                    | Nội dung                                                                      |
|-------------------------|-------------------------------------------------------------------------------|
| `app-settings.json`     | Giao diện, ngôn ngữ, chu kỳ, từ khóa, Selector gần nhất, tab gần nhất, loại trình duyệt, tùy chọn khay/thông báo/phím tắt/chống click trùng |
| `selectors.json`        | Danh sách Selector đã lưu theo tên                                            |
| `browser-settings.json` | Đường dẫn `.exe` của trình duyệt tự chọn                                      |

Bản cũ dùng `ui-settings.json`; lần chạy đầu tiên sẽ tự đọc tệp đó nếu chưa có `app-settings.json`.
Nếu `selectors.json` bị hỏng, ứng dụng sao lưu thành `selectors.json.corrupt-<thời gian>` rồi bắt đầu
với danh sách trống.

## Khắc phục sự cố

| Thông báo trong nhật ký | Nguyên nhân và cách xử lý |
|-------------------------|---------------------------|
| Không tìm thấy trình duyệt hợp lệ đang mở cổng kết nối 9222 | Chưa có trình duyệt mở cổng debug. Bấm **Mở trình duyệt** rồi **Quét tab**. |
| Cổng 9222 đang thuộc trình duyệt không khớp lựa chọn hiện tại | Một trình duyệt khác đang giữ cổng 9222. Đóng nó, hoặc chọn đúng loại trình duyệt trong danh sách. |
| Trình duyệt đang chạy nhưng chưa mở cổng kết nối | Trình duyệt đã mở sẵn mà không có cổng debug. Ứng dụng sẽ hỏi có đóng để mở lại không (tab chưa lưu có thể mất). Nếu trình duyệt chạy nền (biểu tượng ở khay), hãy thoát hẳn rồi thử lại. |
| Đã gọi lệnh mở nhưng cổng kết nối chưa sẵn sàng | Trình duyệt còn chạy nền từ phiên trước, hoặc phiên bản Chromium mới không bật cổng gỡ lỗi với hồ sơ mặc định (xem phần lưu ý bên dưới). |
| Cổng 9222 đã mở nhưng danh tính trình duyệt không khớp lựa chọn | Nhật ký in ra `Browser` và `User-Agent` của trình duyệt đang giữ cổng. Chọn đúng loại trình duyệt, hoặc chọn **Trình duyệt khác** để bỏ qua bước nhận diện. |

## Lưu ý an toàn

- Ứng dụng chỉ kết nối với cổng debug trên `127.0.0.1`. Không mở cổng này ra mạng.
- Khi đang bật cổng debug, bất kỳ chương trình nào trên máy đều có thể điều khiển trình duyệt.
  Chỉ bật khi cần và tắt sau khi dùng.
- Nhật ký chỉ ghi domain và đường dẫn trang, không ghi query string, để tránh lộ token hay tham số phiên.
- Từ Chromium 136, trình duyệt có thể bỏ qua `--remote-debugging-port` khi dùng hồ sơ (user data dir)
  mặc định. Nếu **Mở trình duyệt** luôn báo cổng chưa sẵn sàng, hãy tự mở trình duyệt với hồ sơ riêng rồi
  bấm **Quét tab**, ví dụ:

  ```
  "C:\Đường dẫn\trình duyệt.exe" --remote-debugging-port=9222 --user-data-dir="C:\Temp\acs-profile"
  ```

  Hồ sơ riêng là hồ sơ trống nên cần đăng nhập lại các trang cần dùng.

## Ghi chú về giao diện

- Các control nhập liệu, danh sách, ô số và thanh cuộn đều tự vẽ, lấy màu từ `UiTheme`
  và được vẽ lại khi đổi chế độ sáng/tối.
- Cửa sổ có kích thước cố định (1180 × 800): không kéo giãn và không phóng to, vẫn có nút thu nhỏ.
- Chế độ màu trang web: khi chọn tab, ứng dụng đặt `prefers-color-scheme` của trang theo chế độ mặc định của Windows.
