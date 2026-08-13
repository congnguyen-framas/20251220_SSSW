# 2026-08-13 — Fix: mở app lên không mặc định active tab Step Component (race condition lúc khởi động)

## Yêu cầu

Nguyên văn (kèm 2 ảnh chụp màn hình sau khi tôi trả lời — lần trước — rằng code
đã có `finally { SelectStep(); }` nên "về lý thuyết" đã mặc định mở Step):

> vẫn không hiển thị mặc định active form cân step
> tìm nguyên nhân và khắc phục triệt để
> sau khi xong log secssion và log file

Ảnh 1: app chạy thật, header hiện đúng title (`fVN – Scan and Scale Shot
Weight (SSSW) – Ver-1.0.813.2217`) nhưng **body hoàn toàn trống**, cả 2
RadioButton "Step Component"/"Finished Goods" đều **không có nút nào được
tô đỏ (checked)**. Ảnh 2: Visual Studio đang debug, breakpoint dừng ngay
trong `SelectStep()` (dòng 209, `MainViewModel.cs`) — xác nhận `SelectStep()`
CÓ được gọi tới.

## Nguyên nhân gốc (xác nhận qua log thật, không chỉ suy đoán từ code)

Đối chiếu `Logs/SSSW_Log_20260813.txt` (đúng file/session người dùng vừa
chạy — build lúc 22:17:57, các lần chạy sau đó lúc 22:18, 22:24, 22:27) phát
hiện: mọi lần khởi động app đều thấy `MainViewModel.StartupAsync()` chạy
xong trọn vẹn — mesocomp/mesoyear + `SELECT TOP(1) ... FROM FT608` (Config)
đều log thành công — nhưng **có những lần chạy hoàn toàn KHÔNG thấy câu
query kế tiếp mà `ShotWeightViewModel.InitializeAsync()`/`LoadDataAsync()`
lẽ ra phải tự bắn ra** (`sp_MaterialGetCompanyName` + `sp_MaterialGetMesoyear`
+ `SELECT ... FROM FT601` ngay sau đó) — nghĩa là `ShotWeightWindow.OnLoaded`
**không hề bắn**, dù `ActiveContent`/`SelectStep()` đã chạy xong theo code.
Ở 1 lần chạy khác, đúng chuỗi 3-query này lại xuất hiện — nhưng **trễ gần 2
phút** sau khi Config load xong, đúng thời điểm khớp với việc người dùng tự
tay bấm lại nút "Step Component" khi thấy màn hình trống (một RadioButton
Click bình thường từ tay người dùng luôn chạy trên UI thread nên luôn thành
công) trong lúc đang debug. → Đây là **race condition không ổn định**, không
phải lỗi logic cố định — giải thích vì sao đôi lúc mở app vẫn thấy Step tab
bình thường (khi DB trả lời chậm hơn 1 chút), đôi lúc lại trống trơn (khi DB
trả lời cực nhanh, như log cho thấy: mesocomp/mesoyear chỉ mất 4-15ms).

**Cơ chế chính xác**: `MainViewModel`'s constructor tự bắn `_ =
StartupAsync();` — nhưng constructor này chạy NGAY TRONG lúc
`scope.ServiceProvider.GetRequiredService<Main>()` ở
[Program.cs](../../Program.cs), tức là **TRƯỚC** dòng `wpfApp.Run(wpfWin);`
kế tiếp — nghĩa là trước khi WPF `Application.Run()` kịp `Show()` cửa sổ và
khởi động Dispatcher message pump. Vì các stored procedure
`sp_MaterialGetCompanyName`/`sp_MaterialGetMesoyear` và query `FT608` trên
LAN nội bộ trả lời rất nhanh (log đo được 4–112ms), toàn bộ `StartupAsync()`
— kể cả `SelectStep()` trong `finally` (resolve `ShotWeightWindow` qua DI,
gán vào `ActiveContent`) — có thể **chạy xong trước khi `Application.Run()`
kịp gọi `Show()`**. Kết quả: `ActiveContent` được gán đúng object, binding
kỹ thuật vẫn "đúng", nhưng `ShotWeightWindow` (UserControl) được tạo/gắn vào
cây visual trong lúc cây đó **chưa hề được kết nối vào bất kỳ
PresentationSource (HWND) nào** — nên `Loaded` của nó (nơi
`InitializeAsync()`/kết nối driver/load FT601 được kích hoạt) không bao giờ
bắn ra ở lần gắn đầu tiên đó. Đây khớp 100% với 2 triệu chứng quan sát được:
(1) body trống (nội dung có "tồn tại" về mặt binding nhưng chưa từng thực sự
`Loaded`/render dữ liệu), (2) RadioButton không được tô đỏ dù `IsStepActive`
đã set `true` — vì cùng nằm trong chuỗi bị ảnh hưởng bởi cùng 1 lần "gắn vào
cây visual" bất thường đó.

## Fix

Nguyên tắc: **không tự kích hoạt `StartupAsync()` (và mọi việc tạo
Window/UserControl con của nó) từ bên trong constructor của ViewModel** —
đúng pattern đã dùng nhất quán cho `ShotWeightWindow`/`ShotWeightFGWindow`
(chúng tự gọi `InitializeAsync()` trong sự kiện `Loaded` của chính chúng,
không phải trong constructor của ViewModel). Áp dụng lại đúng pattern đó cho
`Main`/`MainViewModel`.

- **[SSSW/UI/WPF/ViewModels/MainViewModel.cs](../../UI/WPF/ViewModels/MainViewModel.cs)**:
  - Xoá `_ = StartupAsync();` khỏi constructor.
  - Đổi `private async Task StartupAsync()` → `public async Task
    StartupAsync()` để `Main.xaml.cs` gọi được từ ngoài; thêm đoạn XML doc
    giải thích chi tiết race condition ở trên (để không ai vô tình chuyển
    lại về constructor trong tương lai).
  - `catch { }` (rỗng, nuốt exception hoàn toàn không log) khi Config/hardware
    load lỗi → đổi thành `catch (Exception ex) { _logger.LogError(ex, ...); }`
    — trước đây nếu DB/hardware lỗi lúc khởi động, log file không hề có dấu
    vết gì, không thể chẩn đoán.
  - Bọc thêm `try/catch` quanh chính lệnh gọi `SelectStep()` trong `finally`,
    log lỗi nếu có: vì `StartupAsync()` được gọi kiểu fire-and-forget (`_ =
    vm.StartupAsync()` — xem bên dưới), một exception ném ra từ `SelectStep()`
    (VD DI resolve `ShotWeightWindow` thất bại) mà không được bắt riêng sẽ
    biến thành Unobserved Task Exception và bị nuốt hoàn toàn im lặng — đúng
    kiểu triệu chứng "trống trơn, không gì trong log" nếu xảy ra lần nữa vì
    lý do khác trong tương lai.
- **[SSSW/UI/WPF/Main.xaml.cs](../../UI/WPF/Main.xaml.cs)**:
  - Thêm `Loaded += Main_Loaded;` vào constructor không tham số.
  - Thêm handler `Main_Loaded` (guard bằng `_startupTriggered` chống bắn lại
    nếu `Loaded` fire nhiều lần) gọi `await vm.StartupAsync();` — đảm bảo
    toàn bộ chuỗi Config-load → hardware-connect → `SelectStep()` chỉ bắt đầu
    SAU KHI chính `Main` window đã `Loaded` (tức là sau khi
    `Application.Run()` đã `Show()` cửa sổ và message pump đang chạy), loại
    bỏ hẳn khả năng race ở trên.

## Quyết định / giả định

- **Không dùng `ContentRendered` hay `SourceInitialized`, dùng `Loaded`**:
  nhất quán với pattern đã có sẵn ở `ShotWeightWindow`/`ShotWeightFGWindow`
  (cả 2 đều dùng `Loaded`) — không có lý do đặc biệt để `Main` dùng event
  khác; `Loaded` đã đủ đảm bảo cây visual được gắn vào PresentationSource.
- **Giữ nguyên toàn bộ nội dung/logic bên trong `StartupAsync()`** — chỉ đổi
  NƠI nó được gọi (từ constructor → Loaded event) và ĐỘ HIỂN THỊ (private →
  public) + thêm logging; không đổi thứ tự Config-load/hardware-connect/
  SelectStep() bên trong, vì thứ tự đó đã đúng, vấn đề chỉ nằm ở THỜI ĐIỂM
  toàn bộ chuỗi này được kích hoạt so với `Application.Run()`.
- **Giữ `_startupTriggered` guard dù `Window.Loaded` thường chỉ bắn 1 lần**:
  phòng hờ trường hợp window bị ẩn/hiện lại (không xảy ra trong flow hiện tại
  vì app chỉ có 1 cửa sổ `Main` duy nhất và đóng nó = thoát app), theo đúng
  tinh thần các guard `_initialized` đã dùng ở `ShotWeightWindow`/
  `ShotWeightFGWindow`.
- **Log lỗi bằng `_logger.LogError` (Serilog qua `ILogger<MainViewModel>`
  đã có sẵn từ phiên trước)** thay vì hiện `MessageBox` — nhất quán với cách
  các lỗi nền khác trong ViewModel (VD `GetDataHydraAsync`) đang xử lý; lỗi
  lúc khởi động Config/hardware không critical tới mức phải chặn operator
  bằng popup (họ vẫn thấy được UI, tab Step vẫn mở, chỉ là Config/hardware
  có thể chưa đúng — đã có comment cũ giải thích lý do không crash app).

## Việc còn mở

- Chưa chạy lại thực tế trên máy người dùng để xác nhận race condition đã
  hết hẳn (chỉ build-verify + suy luận từ log thật của chính họ) — cần người
  dùng chạy lại vài lần (mở/đóng app liên tục vài lần) để xác nhận Step tab
  luôn hiện đúng mặc định, không còn xảy ra tình trạng trống trơn nữa.
- Nếu race condition vẫn tái diễn sau fix này (khả năng thấp), log file giờ
  đã đủ chi tiết (`_logger.LogError` ở cả 2 catch mới thêm) để xác định
  chính xác exception nào đang xảy ra, thay vì phải suy luận gián tiếp qua
  việc "thiếu 1 dòng log" như lần này.

## Build/test

- `dotnet build SSSW/SSSW.sln` (Debug) — **build thành công, 0 lỗi**, 754
  warning (không đổi so với baseline).
- Chẩn đoán dựa trên phân tích `Logs/SSSW_Log_20260813.txt` thật (không phải
  suy đoán thuần code review) — xem mục "Nguyên nhân gốc" ở trên.
