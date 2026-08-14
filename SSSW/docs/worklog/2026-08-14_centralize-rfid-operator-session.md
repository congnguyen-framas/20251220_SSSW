# Centralize RFID/operator session lên Main (dùng chung Step & FG)

## Yêu cầu

Người dùng (dựa trên IDE selection `ShotWeightWindow.xaml.cs` + screenshot 2 tab Step
Component / Finished Goods) yêu cầu 2 việc liên quan:

1. RFID dùng chung cho cả 2 form cân Step và FG, nên khi đã quét thẻ rồi thì chuyển qua
   lại giữa các form/tab **không được xóa** thông tin operator đã quét — chỉ xóa khi
   nhấn nút Cancel trên mỗi form.
2. Chuyển hết logic của các nút nhấn và RFID trên phần header dùng chung về `Main`, và
   xóa hết logic đó khỏi các form cân Step và FG.

## Đã làm

- **Thêm mới `SSSW/UI/WPF/Services/OperatorSessionService.cs`** — singleton (đăng ký ở
  `SSSW/Program.cs`) kế thừa `BaseViewModel`, sở hữu DUY NHẤT state RFID/operator dùng
  chung Step & FG: `OperatorInfo`, `IsOperatorSet`, `RfidStatus`, `RfidCardCode`,
  `RfidName`, `UserName`. Gồm:
  - `Attach(DeviceConnectionService device)` — idempotent (mirror
    `DeviceConnectionService.EnsureInitialized()`), backfill `RfidStatus` rồi subscribe
    `device.RfidChanged`; handler `Device_RfidChanged` bridge qua
    `Dispatcher.Invoke(...)` (thay thế `RfidDriver_DataValueChanged` trước đây nằm
    riêng ở CẢ 2 code-behind).
  - `OnRfidValueChanged(string rfidCode)` / `OnRfidNameEnterAsync(string name)` — logic
    tra cứu FT029 theo IT/QC department + đăng ký operator mới, ported nguyên vẹn từ
    `ShotWeightViewModel` (trước đây bị lặp lại y hệt ở `ShotWeightViewModel`,
    `ShotWeightFGViewModel`, và gần giống ở `frmRfidInputViewModel`).
  - `Clear()` — nơi DUY NHẤT được phép xóa operator info đã quét; gọi từ
    `ExecuteCancel()` của cả 2 tab.
- **`MainViewModel.cs`**: inject `OperatorSessionService`, expose
  `public OperatorSessionService OperatorSession { get; }`; gọi
  `OperatorSession.Attach(_deviceService)` trong `StartupAsync()` ngay sau
  `_deviceService.EnsureInitialized()`; thêm `OpenRfidInputDialog()` (ported từ
  `ShotWeightViewModel.OpenRfidInputDialog()` cũ) — mở dialog nhập tay, áp kết quả qua
  `OperatorSession.OnRfidValueChanged(...)`.
- **`Main.xaml.cs`**: `_tbEmployee_MouseLeftButtonDown` gọi thẳng
  `vm.OpenRfidInputDialog()`, không còn pattern-match theo tab active.
- **`Main.xaml`**: Ellipse trạng thái RFID và label tên nhân viên trên header rebind từ
  `ActiveContent.DataContext.*` sang `OperatorSession.RfidStatus` /
  `OperatorSession.UserName` — header không đổi khi chuyển tab.
- **`frmRfidInputViewModel.cs`**: không tự tra cứu DB nữa — inject
  `OperatorSessionService`, `OnRfidValueChanged()`/`OnRfidNameEnterAsync(name)` giờ là
  wrapper mỏng gọi `_operatorSession.*` rồi copy kết quả về property riêng của dialog
  (`RfidCardCode`/`RfidName`/`UserName`) để `frmRfidInput.xaml.cs` đọc
  `ResultRfidCode`/`ResultRfidName` như cũ sau `ShowDialog()==true`.
- **`ShotWeightViewModel.cs` và `ShotWeightFGViewModel.cs`** (mirror y hệt nhau): inject
  `OperatorSessionService _operatorSession`; xóa field/property local
  `_employeeCode`, `_operatorInfo`, `UserName`, `RfidName`, `RfidStatus`,
  `RfidCardCode`, `ReadOnlyRfid`; xóa method `OnRfidValueChanged`,
  `OnRfidNameEnterAsync`, `OpenRfidInputDialog`; xóa `ClearRfidAction`/
  `FocusRfidNameAction`. Redirect các chỗ dùng còn lại sang service dùng chung:
  Confirm-gate (`!_operatorSession.IsOperatorSet`), stamping `C010`/`C011` lên FT600
  (`_operatorSession.OperatorInfo.C000`/`C001`), `createdBy` (Step), và
  `ExecuteCancel()` giờ chỉ gọi `_operatorSession.Clear()` — vì service dùng chung nên
  Cancel ở tab nào cũng xóa CẢ 2 tab (đúng ý đồ "chuyển tab không xóa, Cancel mới
  xóa").
- **`ShotWeightWindow.xaml.cs` và `ShotWeightFGWindow.xaml.cs`** (mirror): xóa đăng ký
  `_deviceService.RfidChanged += RfidDriver_DataValueChanged` (và unsubscribe), xóa
  backfill `_vm.RfidStatus = ...`, xóa `_vm.FocusRfidNameAction = ...`; xóa hẳn method
  `RfidDriver_DataValueChanged`, `tbRFIDName_KeyDown`, `_txtRFID_KeyDown` (dead sau khi
  UI liên quan bị xóa khỏi XAML). Giữ nguyên `BarcodeDriver_DataValueChanged`,
  `ScaleDriver_DataValueChanged`, `_txtBarcode_KeyDown` — không thuộc phạm vi refactor
  này.
- **`ShotWeightWindow.xaml` và `ShotWeightFGWindow.xaml`** (mirror): xóa khối
  `<!-- RFID + Barcode row -->` (`Border Grid.Row="1" Visibility="Collapsed"`) — UI RFID
  cũ vốn đã ẩn, không còn cần thiết vì header của `Main` đã hiển thị trạng thái/tên
  operator dùng chung. Renumber `Grid.Row` của hàng còn lại xuống 1 bậc, xóa
  `RowDefinition` dư.

## Quyết định / giả định

- `FocusRfidNameAction` không được port sang service mới — nó chỉ từng focus vào ô
  textbox đã bị ẩn (`Visibility="Collapsed"`) nên vốn đã là no-op trong production.
- Phát hiện namespace không nhất quán giữa các file entity dưới `models/ef/entities/`:
  hầu hết (bao gồm `FT029_Operator_RFID.cs`) khai báo `namespace SSSW.models` (1 chữ
  s), nhưng `DbContextDogeWH.cs` lại khai báo `namespace SSSW.modelss` (2 chữ s) — mô
  tả trong `CLAUDE.md` ("`models/ef/entities/` dùng namespace `SSSW.modelss`") chỉ
  đúng cho `DbContextDogeWH` và `FT605.cs`, không đúng cho phần lớn entity còn lại.
  `OperatorSessionService.cs` (dùng cả `FT029_Operator_RFID` lẫn `DbContextDogeWH`) cần
  CẢ HAI using (`SSSW.models` và `SSSW.modelss`) — giống pattern đã có sẵn ở
  `ShotWeightViewModel.cs`. Không sửa lại namespace của các file entity (theo đúng ghi
  chú trong `CLAUDE.md`: đây là convention hiện có, không phải lỗi cần tự sửa).

## Việc còn mở

- Không có test project trong repo (theo `CLAUDE.md`) nên phần verify luồng RFID
  scan/switch-tab/Cancel chỉ làm được bằng code-inspection (đã grep xác nhận: gate
  Confirm, stamping C010/C011, `Clear()` trong `ExecuteCancel()`, binding header —
  xem mục Build/test), chưa chạy tay trên máy có phần cứng RFID thật.

## Build/test

- `dotnet build SSSW/SSSW.sln` (Debug): **0 Error(s)**, build sạch.
- Grep xác nhận không còn tham chiếu chết tới `RfidDriver_DataValueChanged`,
  `FocusRfidNameAction`, `ClearRfidAction` (trừ bản local hợp lệ trong
  `frmRfidInputViewModel.cs`), `ReadOnlyRfid`, `_txtRFID_KeyDown`, `tbRFIDName` trong
  cả code-behind lẫn XAML của Step/FG.
- Release build (kèm `CopyUpdateToShare`) chưa chạy — theo `CLAUDE.md`, bước này dự
  kiến fail khi máy dev không có quyền truy cập network share, không phải lỗi code.
