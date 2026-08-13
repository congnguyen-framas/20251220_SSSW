# 2026-08-13 — Fix: click vào label "RFID - Employee ID" không mở dialog nhập tay; Unlock nút Hydra/Settings/Update trên tab FG

## Yêu cầu

Nguyên văn (kèm 2 ảnh chụp màn hình — 1 ảnh khoanh mũi tên chỉ vào vùng label
"RFID - Employee ID:" trên tab FG, 1 ảnh chụp cận nhóm nút icon Reload/History/
Hydra/Settings/Update):

> click chuột vào _tbEmployee nhưng ko mở form nhập RFID bằng tay, tìm nguyên
> nhân và xử lý triệt để.
> Mở khóa các nút này khi đang chọn cân FG, cả step và FG đều nhấn được các
> nút này

Đây là 2 bug độc lập, cùng nằm trên header của `Main.xaml` (đã gộp từ 2 view
Step/FG lên Main ở phiên trước — xem
[2026-08-13_main-shell-step-fg-tabs.md](2026-08-13_main-shell-step-fg-tabs.md)).

## Đã làm

### Bug 1 — Click vào `_tbEmployee` không mở dialog nhập tay RFID

**Nguyên nhân gốc**: `_tbEmployee` là 1 `TextBlock` không có `Background`
(mặc định null). WPF chỉ hit-test đúng vùng glyph chữ khi `Background` là
null/không set — phần bounding box còn lại (kể cả toàn bộ control khi `Text`
rỗng) hoàn toàn "trong suốt" với chuột. `Text` của `_tbEmployee` bind vào
`ActiveContent.DataContext.UserName` — khi chưa quét RFID lần nào (UserName
rỗng), TextBlock không có glyph nào để hit-test → click lọt thẳng xuống
`Border` cha (chứa toàn bộ header) đang có `MouseLeftButtonDown=
"TitleBar_MouseLeftButtonDown"` (kéo-thả cửa sổ) thay vì trigger handler của
chính `_tbEmployee`. Đây là lý do "triệt để" khớp với hiện tượng: bấm vào chỗ
đúng vị trí label nhưng không có gì xảy ra (vì thực chất tại thời điểm đó
không hề có gì ở đó để bấm).

Bug phụ đi kèm (không phải nguyên nhân chính nhưng cùng gây khó chịu): kể cả
khi `_tbEmployee` CÓ text để bấm trúng, `MouseLeftButtonDown` là routed event
kiểu Bubble — sau khi handler của `_tbEmployee` chạy xong (mở dialog modal),
sự kiện tiếp tục nổi lên `Border` cha vì không có ai set `e.Handled = true`,
kích hoạt `TitleBar_MouseLeftButtonDown` (kéo-thả cửa sổ) ngay sau khi dialog
đóng — không liên quan trực tiếp đến bug chính nhưng là hành vi sai cần chặn.

**Fix** — [SSSW/UI/WPF/Main.xaml](../../UI/WPF/Main.xaml):
- Thêm `Background="Transparent"` cho `_tbEmployee` — để toàn bộ bounding box
  luôn hit-test được bất kể `Text` có rỗng hay không.
- Thêm `MinWidth="24"` — đảm bảo luôn có 1 vùng bấm tối thiểu ngay cả khi
  `Text` rỗng (Transparent Background không tự cho control 1 kích thước, vẫn
  cần Width/Height > 0 để có gì đó để hit-test).

**Fix bug phụ** — [SSSW/UI/WPF/Main.xaml.cs](../../UI/WPF/Main.xaml.cs):
- Thêm `e.Handled = true;` đầu `_tbEmployee_MouseLeftButtonDown` — chặn hẳn
  việc sự kiện nổi tiếp lên `Border` cha.

### Bug 2 — Nút Hydra/Settings/Update "khoá" (không có tác dụng) khi đang ở tab FG

**Nguyên nhân gốc**: 3 nút icon Hydra/Settings/Update trong nhóm "ACTION ICON
BUTTONS" của `Main.xaml` bind `Command="{Binding
ActiveContent.DataContext.HydraCommand}"` (tương tự cho Settings/Update) —
nhưng `HydraCommand`/`SettingsCommand`/`UpdateCommand` trước đây CHỈ tồn tại
trên `ShotWeightViewModel` (tab Step), KHÔNG tồn tại trên
`ShotWeightFGViewModel` (tab FG). Khi tab FG đang active, biểu thức binding
`ActiveContent.DataContext.HydraCommand` resolve ra `null` (binding lỗi thầm
lặng, không throw, chỉ log warning trong Output window) → `Button.Command ==
null` → bấm nút không có tác dụng gì (không exception, không hiệu ứng —
đúng như mô tả "khoá"). 2 nút Reload/History không bị ảnh hưởng vì cả 2
ViewModel đều tự có `ReloadCommand`/`HistoryViewCommand` riêng.

**Fix**: Nhận thấy Hydra (đồng bộ FT601 từ Hydra ERP)/Settings (mở
`frmUpdateMasterData` — master data dùng chung)/Update (AutoUpdaterDotNET)
đều là hành động **cấp APP**, không thuộc riêng Step hay FG (FT601 là bảng
dùng chung, master data dùng chung, auto-update là hạ tầng app-wide) — nên
chuyển hẳn 3 command này (và toàn bộ AutoUpdater plumbing đi kèm) từ
`ShotWeightViewModel` lên `MainViewModel`, đúng tinh thần kiến trúc đã thiết
lập ở phiên trước (Main là chủ sở hữu duy nhất của các mối quan tâm cấp app).

- **[SSSW/UI/WPF/ViewModels/MainViewModel.cs](../../UI/WPF/ViewModels/MainViewModel.cs)**:
  thêm `ILogger<MainViewModel>` (constructor injection), field `_isUpdateClicked`,
  3 command `HydraCommand`/`SettingsCommand`/`UpdateCommand`, và copy nguyên
  vẹn method `GetDataHydraAsync`/`OpenSettings`/`CheckUpdate`/
  `AutoUpdater_ApplicationExitEvent`/`AutoUpdater_CheckForUpdateEvent` từ
  `ShotWeightViewModel`. 2 chỗ khác duy nhất so với bản gốc:
  - `GetDataHydraAsync`/`OpenSettings`: bước reload cuối cùng
    (`await LoadDataAsync()` / `_ = LoadDataAsync(...)` — vốn chỉ reload riêng
    Step) đổi thành `RefreshActiveContent()` (method mới) — pattern-match
    `ActiveContent`'s `DataContext` (giống hệt cách `_tbEmployee_
    MouseLeftButtonDown` đang làm) rồi gọi `ReloadCommand.Execute(null)` của
    ĐÚNG ViewModel đang active (Step hoặc FG) — vì giờ Hydra sync/Settings áp
    dụng cho dữ liệu dùng chung, tab nào đang mở cũng cần thấy dữ liệu mới.
  - AutoUpdater init (`RunUpdateAsAdmin`/`DownloadPath`) + subscribe 2 event
    tĩnh chuyển từ `ShotWeightViewModel.InitializeAsync()` (chạy mỗi lần Step
    tab load) sang `MainViewModel` constructor (chạy đúng 1 lần duy nhất khi
    app khởi động, không phụ thuộc tab nào được mở trước).
- **[SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs](../../UI/WPF/ViewModels/ShotWeightViewModel.cs)**:
  xoá hẳn 3 property command, method `GetDataHydraAsync`/`OpenSettings`/
  `CheckUpdate`/2 AutoUpdater handler, field `_isUpdateClicked`/
  `_hydraItemDetails`, 4 dòng AutoUpdater init/subscribe trong
  `InitializeAsync()`, và `using AutoUpdaterDotNET;` (không còn dùng).
- **[SSSW/UI/WPF/Main.xaml](../../UI/WPF/Main.xaml)**: đổi
  `Command="{Binding ActiveContent.DataContext.HydraCommand}"` (và Settings/
  Update) thành `Command="{Binding HydraCommand}"` (bind thẳng vào
  `MainViewModel`, không qua `ActiveContent.DataContext` nữa) — Reload/
  History giữ nguyên bind qua `ActiveContent.DataContext.*` vì vẫn là hành
  động riêng theo tab.

## Quyết định / giả định

- **Chuyển hẳn Hydra/Settings/Update lên Main thay vì chỉ thêm 3 property
  tương ứng vào `ShotWeightFGViewModel`** (phương án thay thế đơn giản hơn):
  chọn phương án chuyển lên Main vì tránh trùng lặp ~100 dòng logic
  `GetDataHydraAsync` (insert FT601) và toàn bộ AutoUpdater plumbing giữa 2
  ViewModel — nếu copy sang FG sẽ phải duy trì 2 bản giống hệt nhau mỗi lần
  sửa sau này. Đồng thời đúng bản chất dữ liệu: FT601/master data/auto-update
  không phải thứ "thuộc về" Step hay FG, mà dùng chung toàn app — khớp với
  quyết định kiến trúc đã chốt ở phiên trước (Main sở hữu mọi mối quan tâm
  cấp app, ViewModel con chỉ giữ business logic cân).
- **`RefreshActiveContent()` gọi qua `ReloadCommand.Execute(null)`** (không
  gọi thẳng `LoadDataAsync()`/tương đương): vì `MainViewModel` không có
  reference kiểu cụ thể đến method private của 2 ViewModel con, chỉ có
  `ActiveContent` (kiểu `object?`, thực chất là `UIElement`/`FrameworkElement`)
  — pattern-match ra đúng ViewModel rồi gọi `ICommand.Execute(null)` public
  sẵn có là cách ít xâm lấn nhất, không cần thêm interface chung hay đổi
  visibility của `LoadDataAsync()`.
- **`e.Handled = true` trong `_tbEmployee_MouseLeftButtonDown`**: fix "triệt
  để" theo đúng yêu cầu — dù đây là bug phụ (không phải nguyên nhân chính
  khiến dialog không mở), để lại sẽ gây thêm 1 bug ẩn khác (window tự kéo-thả
  ngay sau khi đóng dialog RFID) nếu không chặn.
- **Không đổi hành vi `CheckUpdate`/`AutoUpdater_CheckForUpdateEvent`** (giữ
  nguyên message "Already up to date." chỉ hiện khi `_isUpdateClicked`) — copy
  y nguyên logic cũ, chỉ đổi nơi ở.

## Việc còn mở

- Chưa test thực tế: (1) bấm label RFID lúc chưa quét thẻ (UserName rỗng) có
  mở được dialog nhập tay trên cả Step lẫn FG; (2) bấm Hydra/Settings/Update
  lúc đang ở tab FG có chạy đúng và tab FG tự refresh grid sau đó; (3) bấm
  Hydra/Settings lúc đang ở tab Step vẫn hoạt động như cũ (không regression).
- Nút Reload/History vẫn cố tình giữ nguyên bind theo `ActiveContent.
  DataContext.*` (không đụng tới) — không nằm trong phạm vi báo lỗi lần này.

## Build/test

- Gặp lỗi build tạm thời `MC1000 ... file ... being used by another process`
  trên `ShotWeightWindow.g.cs` — do MSBuild/VBCSCompiler server cũ còn giữ
  handle từ lần build trước; chạy `dotnet build-server shutdown` rồi build lại
  là hết, không phải lỗi code.
- `dotnet build SSSW/SSSW.sln` (Debug) — **build thành công, 0 lỗi**, 754
  warning (không đổi so với baseline, toàn bộ pre-existing).
- Chưa test chạy thực tế trên máy có hardware.
