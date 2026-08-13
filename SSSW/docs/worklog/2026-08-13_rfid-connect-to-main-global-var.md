# 2026-08-13 — Chuyển kết nối RFID (Config + hardware) ra Main; thêm biến toàn cục GlobalVariable.Devices

## Yêu cầu

Nguyên văn:

> chuyển phần kết nối thiết bị RFID ra Main. và tạo biến toàn cục để các form
> khác lấy dùng

Trước đó, DeviceConnectionService (singleton, sở hữu Barcode/RFID/Scale driver
— thêm từ commit `ac887a9`) được ShotWeightWindow/ShotWeightFGWindow nhận qua
constructor DI, và lệnh kết nối thật (`EnsureInitialized()`) chỉ được gọi từ
`ShotWeightViewModel.InitializeAsync()` (qua delegate `ConnectHardwareAction`)
— tức là gắn liền với tab Step, chạy SAU khi tab đó tự load xong
`GlobalVariable.ConfigSystem` từ DB.

Do câu yêu cầu ngắn, có 3 điểm cần chốt trước khi sửa (hỏi qua
`AskUserQuestion`, người dùng chọn cả 3 phương án khuyến nghị):

1. Gộp luôn việc load Config vào Main (không chỉ tách lệnh connect) — vì RFID
   chỉ connect đúng khi Config (IP/COM) đã có, tách nửa vời sẽ cần thêm cơ chế
   chờ phức tạp.
2. Biến toàn cục đặt trên `GlobalVariable` (cùng chỗ với `ConfigSystem` sẵn
   có) — không tạo static field riêng trên `DeviceConnectionService`.
3. Consumer hiện tại chỉ cần 2 View WPF (Step/FG) — chưa cần đụng tới WinForms
   legacy (`frmShotWeightScale`/`frmShotWeightScaleV2`).

## Đã làm

- **`SSSW/staticClass/GlobalVariable.cs`**: thêm
  `public static DeviceConnectionService? Devices { get; set; }` (cùng chỗ
  với `ConfigSystem`) + `using SSSW.UI.WPF.Services;`. Gán bởi
  `MainViewModel` một lần duy nhất, đọc bởi `ShotWeightWindow`/
  `ShotWeightFGWindow` thay vì nhận qua constructor DI.
- **`SSSW/UI/WPF/ViewModels/MainViewModel.cs`**: constructor nhận thêm
  `DeviceConnectionService deviceService` (đã là DI singleton sẵn có, không
  cần đăng ký thêm ở `Program.cs`) và gán `GlobalVariable.Devices =
  _deviceService;` NGAY (đồng bộ, trong constructor) — để không bao giờ null
  dù driver có kết nối xong hay chưa. Đổi `LoadWindowTitleAsync()` thành
  `StartupAsync()` — vẫn tính `WindowTitle` như cũ, nhưng nối thêm 2 việc
  (chuyển nguyên logic từ `ShotWeightViewModel.InitializeAsync()`):
  1. Load `mesoYear` (trước chỉ có `mesocomp`) + Config từ `FT608_Config`
     (`WHERE c000 = MachineName`, insert default nếu chưa có row) →
     `GlobalVariable.ConfigSystem`.
  2. Gọi `_deviceService.EnsureInitialized()` — kết nối Barcode/RFID/Scale
     bằng đúng Config vừa load (bắt buộc chạy SAU bước 1).
  `SelectStep()` (mở tab mặc định) dời từ constructor xuống `finally` của
  `StartupAsync()` — chỉ mở tab Step SAU khi Config/hardware đã sẵn sàng (hoặc
  sau khi catch lỗi DB, để không kẹt màn hình trống nếu DB tạm lỗi lúc khởi
  động).
- **`SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs`**: xóa hẳn block load
  `FT608_Config`/insert-default (đã chuyển lên Main) và property
  `ConnectHardwareAction` + lời gọi `ConnectHardwareAction?.Invoke()` trong
  `InitializeAsync()` — không cần nữa vì tới lúc View này `Loaded` thì Main đã
  connect xong. Giữ nguyên `_mesocomp`/`_mesoYear` (vẫn dùng để gán
  `Mesocomp`/`Mesoyear` khi ghi `FT600`).
- **`SSSW/UI/WPF/ShotWeightWindow.xaml.cs`** +
  **`SSSW/UI/WPF/ShotWeightFGWindow.xaml.cs`**: bỏ tham số
  `DeviceConnectionService deviceService` khỏi constructor DI — `OnLoaded`
  giờ tự lấy `_deviceService = GlobalVariable.Devices!;` ở bước đầu tiên
  trước khi đăng ký event/backfill status (logic backfill + gọi lại
  `EnsureInitialized()` — luôn no-op vì Main đã connect — giữ nguyên như cũ,
  chỉ đổi nguồn lấy `_deviceService`).

## Quyết định / giả định

- **`SelectStep()` dời xuống sau khi Config/hardware load xong** (thay vì gọi
  ngay trong constructor như trước): hệ quả trực tiếp của việc gộp Config vào
  Main — nếu vẫn mở tab Step ngay lập tức như cũ, `ShotWeightWindow` có thể
  `Loaded` (và chạy `InitializeAsync()`) trước khi `GlobalVariable.ConfigSystem`
  kịp load xong (race), khiến `UsagePct`/`ReadOnlyRfid`/... đọc nhầm giá trị
  mặc định. Đánh đổi: body `Main` trống trong khoảnh khắc đầu (một round-trip
  DB) lúc khởi động — chấp nhận được vì hành vi tương đương bản cũ (trước khi
  có `Main`, cả app cũng phải đợi y hệt round-trip này trước khi
  `ShotWeightWindow` hiện được nội dung).
- **`GlobalVariable.Devices` gán đồng bộ trong constructor của
  `MainViewModel`** (không đợi `StartupAsync()`): vì `SelectStep()`/
  `SelectFg()` (resolve `ShotWeightWindow`/`ShotWeightFGWindow` qua DI) chỉ
  chạy sau khi `StartupAsync()` đã gán xong (do dời xuống `finally`), nên thực
  tế không còn race — nhưng vẫn gán sớm cho an toàn/rõ ràng ý đồ (không ai đọc
  `GlobalVariable.Devices` sẽ thấy null).
- **Không đổi DeviceConnectionService** (giữ nguyên singleton +
  `EnsureInitialized()` idempotent bọc all-in-one Barcode/RFID/Scale, không
  tách riêng RFID) — yêu cầu gốc nói "kết nối thiết bị RFID" nhưng 3 driver
  này vốn được kết nối cùng lúc trong 1 method
  (`DeviceConnectionService.ApplyHardwareConfig()`), tách riêng RFID ra khỏi
  Barcode/Scale sẽ phá cấu trúc hiện có mà không có lý do rõ ràng — hiểu là
  "chuyển **lệnh gọi** kết nối (bao gồm RFID) lên Main", không phải "chỉ kết
  nối mỗi RFID mà bỏ Barcode/Scale".
- **`ShotWeightWindow`/`ShotWeightFGWindow` vẫn tự gọi lại
  `_deviceService.EnsureInitialized()`** trong `OnLoaded` (không xóa dòng
  này dù luôn là no-op sau khi Main đã connect): giữ làm safety net — nếu sau
  này có luồng nào khác mở 2 View này trước khi Main kịp connect (hiện tại
  không xảy ra vì `SelectStep()` dời xuống sau `StartupAsync()`), code vẫn tự
  phục hồi thay vì NullReferenceException hay driver không bao giờ được
  connect.

## Việc còn mở

- Chưa test chạy thực tế trên máy có hardware RFID/Barcode/Scale — chỉ mới
  build (compile-time) thành công. Cần xác nhận: app mở lên tab Step đúng như
  cũ (chỉ trễ hơn một chút vì đợi Config trước), đèn trạng thái RFID chuyển
  Connected đúng, chuyển qua tab FG vẫn thấy RFID đã kết nối sẵn (không phải
  connect lại), và trường hợp DB lỗi lúc khởi động vẫn mở được tab Step (dù
  Config mặc định) thay vì treo màn hình trống.
- WinForms legacy (`frmShotWeightScale`, `frmShotWeightScaleV2`) chưa được nối
  vào `GlobalVariable.Devices` — ngoài phạm vi lần này theo lựa chọn của người
  dùng ("chỉ 2 ViewModel WPF"); nếu sau này cần, 2 form đó có thể đọc thẳng
  `GlobalVariable.Devices` mà không cần sửa gì thêm ở phía Main.

## Build/test

- Xác nhận không có tiến trình `SSSW.exe` nào đang chạy trước khi build.
- `dotnet build SSSW/SSSW.sln` (Debug) — **build thành công, 0 lỗi**, 754
  warning (không đổi so với baseline trước, toàn bộ pre-existing).
- Chưa test chạy thực tế trên máy có hardware.
