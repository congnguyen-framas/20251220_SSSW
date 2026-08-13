# 2026-08-13 — Main shell với 2 tab Step Component / Finished Goods

## Yêu cầu

Nguyên văn:

> thay đổi cách mở form cân step và FG như sau:
> - Tạo 1 form Main.xaml, chứa phần header và phần body trống
> - trên header add thêm 2 nút: StepCoponent và Finished Goods
>   + nhấn vào nút Step Component thì lấy form ShotWeightWindow.xaml hiển thị lên body và thực hiện logic cân cho step
>   + Nhấn vào nút Finished Goods thì lấy form ShotWeightFGWindow.xaml hiển thị lên body và thực hiện logic cân cho FG.
> - khi mở app lên mặc định mở form cân step

Trước đây `ShotWeightWindow` là `Window` top-level thật của app; nút "Scan FG"
trên title bar của nó mở `ShotWeightFGWindow` như **modal dialog**
(`ShowDialog()`), tạm ngưng nhận event hardware của cửa sổ chính trong lúc đó
(`SuspendDeviceEventsAction`/`ResumeDeviceEventsAction`).

## Đã làm

- **`SSSW/UI/WPF/Styles/ShotWeightStyles.xaml`**: thêm style `NavTabBtn`
  (RadioButton dùng làm nút tab trên header Main — checked = nền đỏ `#C62828`).
- **`SSSW/UI/WPF/ShotWeightWindow.xaml` + `.xaml.cs`**: `Window` → `UserControl`
  (WPF không cho nhúng `Window` vào visual tree của `Window` khác). Bỏ hết
  attribute/chrome chỉ `Window` mới có (`Title`/`Width`/`Height`/`WindowState`/
  `WindowStyle`/`WindowStartupLocation`/`AllowsTransparency`/drag handler/
  Minimize/Maximize/Close buttons) — các thứ này nay thuộc về `Main`. Bỏ luôn
  nút "Scan FG" trên title bar (Main đã có tab riêng). Đổi `Closing`/`OnClosing`
  → `Unloaded`/`OnUnloaded` (cùng logic hủy đăng ký event hardware) — lý do:
  `UserControl` không có sự kiện `Closing`, và `Unloaded` bắn đúng lúc cần
  (mỗi lần Main đổi tab, `ContentControl.Content` được swap ra). Thêm field
  `_initialized`, bọc `await _vm.InitializeAsync()` trong `if (!_initialized)`
  để tránh load lại toàn bộ master data mỗi lần quay lại tab Step (mất dữ liệu
  cân dở nếu load lại). Thêm `using UserControl = System.Windows.Controls.
  UserControl;` (dự án dùng cả `UseWPF` + `UseWindowsForms` nên `UserControl`
  bị ambiguous giữa 2 namespace — lỗi CS0104 khi build).
- **`SSSW/UI/WPF/ShotWeightFGWindow.xaml` + `.xaml.cs`**: chuyển đổi tương tự
  (không có nút "Scan FG" để bỏ vì cửa sổ này vốn không có nó); cùng pattern
  `_initialized` guard quanh `InitializeFgAsync()`, cùng disambiguation `using`.
- **`SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs`**: xóa
  `SuspendDeviceEventsAction`/`ResumeDeviceEventsAction` (không cần nữa — vòng
  đời `Unloaded`/`Loaded` của `UserControl` khi Main đổi tab đã tự đảm bảo chỉ
  tab đang hiển thị mới nhận event hardware), xóa `ScaneFGCommand` (property +
  wiring trong constructor) và toàn bộ method `OpenShotWeightFGWindow()` (dùng
  `win.Owner`/`win.ShowDialog()` — không compile được nữa vì
  `ShotWeightFGWindow` giờ là `UserControl`, không phải `Window`).
- **`SSSW/UI/WPF/ViewModels/MainViewModel.cs`** (file mới): constructor nhận
  `IServiceProvider`. Property `ActiveContent` (nội dung hiện hiển thị ở body),
  `IsStepActive`/`IsFgActive` (bind vào `IsChecked` của 2 RadioButton, OneWay).
  `SelectStepCommand`/`SelectFgCommand` (`RelayCommand`) — mỗi lệnh **resolve
  qua DI một lần rồi cache lại** (`_stepView ??= ...`, `_fgView ??= ...`), không
  tạo instance mới mỗi lần bấm tab → giữ nguyên dữ liệu cân dở khi chuyển qua
  lại. Mặc định gọi `SelectStep()` cuối constructor → mở app luôn vào tab Step.
- **`SSSW/UI/WPF/Main.xaml` + `Main.xaml.cs`** (file mới): `Window` thật duy
  nhất của app (`WindowStyle="None"`, `WindowState="Maximized"`,
  `WindowStartupLocation="CenterScreen"` — kế thừa từ `ShotWeightWindow` cũ).
  Header (`Border` cao 44, nền `#0D1117`) gồm logo, tên app tĩnh, 2
  `RadioButton` (`GroupName="MainNav"`, style `NavTabBtn`) bind
  `SelectStepCommand`/`SelectFgCommand`, rồi Minimize/Maximize/Close (style
  `TitleIconBtn` dùng lại). Body: `<ContentControl Content="{Binding
  ActiveContent}" />`. Code-behind chỉ có Win32 drag + 3 handler
  Minimize/Maximize/Close — copy nguyên logic từ `ShotWeightWindow.xaml.cs`
  bản cũ (trước khi bị bóc ra) qua `git diff`.
- **`SSSW/Program.cs`**: đăng ký `services.AddTransient<MainViewModel>();` và
  `services.AddTransient<Main>();`. Đổi
  `scope.ServiceProvider.GetRequiredService<ShotWeightWindow>()` +
  `wpfApp.Run(wpfWin)` → resolve `Main` thay vì `ShotWeightWindow`. Các đăng ký
  `ShotWeightWindow`/`ShotWeightViewModel`/`ShotWeightFGWindow`/
  `ShotWeightFGViewModel` giữ nguyên (MainViewModel vẫn resolve chúng qua
  `IServiceProvider`).
- **`SSSW/docs/worklog/INDEX.md`** (file mới — bản cũ đã bị xóa ở commit
  `f9b0eb7`, tạo lại theo đúng convention cũ vì `CLAUDE.md` vẫn yêu cầu duy trì
  worklog).

## Quyết định / giả định

- **Cache view thay vì tạo mới mỗi lần chuyển tab**: yêu cầu gốc không nói rõ,
  nhưng hành vi cũ (đóng dialog FG quay lại Step y nguyên trạng thái) là
  baseline hợp lý nhất để không phá vỡ kỳ vọng — tạo instance mới mỗi lần bấm
  tab sẽ âm thầm mất dữ liệu cân dở của operator trên sàn sản xuất.
- **Bỏ hẳn `ScaneFGCommand`/nút "Scan FG" trên title bar của `ShotWeightWindow`**:
  vì Main đã có tab "Finished Goods" làm entry point duy nhất — giữ lại nút cũ
  sẽ là đường vào thứ 2 dẫn tới flow modal không còn tồn tại (không compile
  được nữa). Chưa hỏi lại người dùng, xem là hệ quả tất yếu của yêu cầu đổi
  cách mở form.
- **`MinimizeCommand`/`MaximizeCommand`/`CloseCommand` trong
  `ShotWeightViewModel`**: phát hiện có vẻ là dead code từ trước (không còn
  binding nào trong XAML dùng tới sau khi bỏ title bar chrome) — **cố ý không
  xóa**, ngoài phạm vi yêu cầu lần này.
- **Tái tạo `docs/worklog/INDEX.md`**: file này từng bị xóa ở một commit trước
  ("Updated worklog docs: removed/deleted outdated entries") nhưng `CLAUDE.md`
  vẫn mô tả nó là quy trình bắt buộc — ưu tiên theo `CLAUDE.md` (được đánh dấu
  override mọi hành vi mặc định) thay vì suy đoán ý định xóa trước đó.

## Việc còn mở

- Chưa test chạy thực tế trên máy có kết nối hardware (barcode/RFID/scale) —
  chỉ mới build thành công (compile-time), chưa xác nhận: app mở mặc định vào
  tab Step + maximize đúng; chuyển qua Finished Goods rồi quay lại Step giữ
  nguyên state; event hardware chỉ tác động tab đang hiển thị; Minimize/
  Maximize/Close trên `Main` hoạt động đúng.
- Chưa cập nhật `ShotWeightWindow_Documentation_EN.md` với kiến trúc Main mới
  (việc này nằm trong todo của phiên nhưng ưu tiên build xanh trước).

## Build/test

- `dotnet build SSSW/SSSW.sln` (Debug) — **build thành công, 0 lỗi** sau khi
  sửa 2 lỗi `CS0104` (`UserControl` ambiguous giữa `System.Windows.Controls`
  và `System.Windows.Forms` — dự án bật cả `UseWPF` và `UseWindowsForms`) bằng
  cách thêm `using UserControl = System.Windows.Controls.UserControl;` vào
  `ShotWeightWindow.xaml.cs` và `ShotWeightFGWindow.xaml.cs`. 754 warning còn
  lại đều pre-existing (nullable warnings, unused Designer fields...), không
  liên quan đến thay đổi lần này.

## Cập nhật 2026-08-13 (tiếp) — Gộp header của từng view lên Main

### Yêu cầu

Người dùng gửi kèm ảnh chụp app đang chạy (sau lần build ở trên), khoanh đỏ
hàng header thứ 2 (ngay dưới header của `Main`) hiển thị `WindowTitle` +
RFID/Employee + 5 nút icon — vẫn còn sót lại từ `ShotWeightWindow`/
`ShotWeightFGWindow` cũ, kèm ghi chú "Bỏ headder của từng form". Nguyên văn:

> Bỏ phần header trong từng from cân đi, vầ đưa hết lên trên header form main

Tức: `ShotWeightWindow`/`ShotWeightFGWindow` khi chuyển thành `UserControl` ở
lần đổi trước vẫn giữ nguyên phần header cũ của chúng (logo, `WindowTitle`,
RFID/Employee, nút Reload/History/Hydra/Settings/Update) — chỉ có phần chrome
window thật (drag/minimize/maximize/close) là chuyển lên `Main`. Lần này gộp
nốt phần header còn lại đó vào `Main`.

### Đã làm

- **`ShotWeightWindow.xaml`/`.xaml.cs`**: xóa hẳn `Border` header (logo,
  `WindowTitle`, RFID/Employee, 5 nút icon) và `Grid.RowDefinitions` (44/`*`)
  tương ứng — nội dung chính (`Grid Margin="6"`) giờ là con duy nhất của Grid
  ngoài cùng, không còn `Grid.Row`. Xóa method `_tbEmployee_MouseLeftButtonDown`
  (không còn phần tử `_tbEmployee` trong file này nữa).
- **`ShotWeightFGWindow.xaml`/`.xaml.cs`**: y hệt (2 nút icon thay vì 5, không
  có `IsEnabled="{Binding ReadonlyRfid}"` — vốn dĩ FG không có binding đó).
- **`Main.xaml`**: thêm 1 hàng header phụ (cao 40, nền `#1B222C`, ngay dưới
  header 44px của Main) chứa `WindowTitle`, RFID/Employee, và các nút icon —
  tất cả bind qua `{Binding ActiveContent.DataContext.*}` thay vì bind trực
  tiếp `{Binding *}` như header gốc của Main — vì `Main.DataContext` là
  `MainViewModel`, còn các property này (`WindowTitle`, `UserName`,
  `RfidStatus`, `ReloadCommand`, ...) nằm trên `ShotWeightViewModel`/
  `ShotWeightFGViewModel` (DataContext của `ActiveContent`). WPF binding engine
  tự re-evaluate cả chuỗi path này mỗi khi `ActiveContent` đổi (RadioButton
  chuyển tab) vì `FrameworkElement.DataContext` là `DependencyProperty` —
  không cần proxy property nào trên `MainViewModel`, không cần interface
  chung giữa 2 ViewModel. `Reload`/`History` luôn hiện; `Hydra`/`Settings`/
  `Update` (chỉ tồn tại trên `ShotWeightViewModel`, FG không có) ẩn hẳn khi
  tab FG active bằng `Visibility="{Binding IsStepActive, Converter=
  {StaticResource BoolToVisibility}}"` — dùng lại converter/property đã có
  sẵn từ lần đổi trước. Đối chiếu `git diff` bản gốc để lấy đúng mã icon glyph
  (`&#xE895;` Hydra, `&#xE777;` Update — ban đầu gõ nhầm `&#xE753;`/`&#xE896;`
  rồi sửa lại theo diff). `LabelStyle` mặc định `Foreground="#37474F"` (cho
  nền sáng) nên phải override `Foreground="White"` tường minh trên các
  TextBlock label/employee vì nền hàng header phụ này tối (`#1B222C`) — nếu
  không sẽ gần như vô hình. Body `ContentControl` dời từ `Grid.Row="1"` sang
  `Grid.Row="2"`.
- **`Main.xaml.cs`**: thêm `_tbEmployee_MouseLeftButtonDown` — vì không có
  interface chung giữa `ShotWeightViewModel`/`ShotWeightFGViewModel`, dùng
  `switch` pattern-matching trên `(vm.ActiveContent as FrameworkElement)?.
  DataContext` để gọi đúng `OpenRfidInputDialog()` của ViewModel đang active.

### Quyết định / giả định

- **Không tạo interface chung cho 2 ViewModel**: giữ đúng tinh thần "diff nhỏ,
  rủi ro thấp" từ lần trước — dùng binding path động (`ActiveContent.
  DataContext.*`) và pattern-matching ở code-behind thay vì refactor kiến trúc.
- **Bỏ `IsEnabled="{Binding ReadonlyRfid}"`** trên TextBlock employee: property
  này không tồn tại ở đâu trong code (đã grep toàn repo xác nhận) — dead
  binding từ trước, nhân dịp gộp header thì bỏ luôn thay vì mang theo.

### Build/test

- `dotnet build SSSW/SSSW.sln` (Debug) — **build thành công, 0 lỗi**, 754
  warning (không đổi so với lần build trước, toàn bộ pre-existing).
- Chưa test chạy thực tế: chưa xác nhận trực quan hàng header phụ hiển thị
  đúng `WindowTitle`/RFID/nút icon theo đúng tab đang active, và bấm vào tên
  nhân viên mở đúng dialog nhập tay Employee ID cho tab hiện tại.

## Cập nhật 2026-08-13 (tiếp #2) — Gộp luôn hàng header phụ vào 1 hàng duy nhất

### Yêu cầu

Người dùng chụp lại app sau bản build ở trên: hàng header phụ (nền `#1B222C`)
vẫn hiện như 1 dải riêng ngay dưới header chính của `Main`, khoanh đỏ lại kèm
2 mũi tên chỉ lên header chính. Nguyên văn:

> đưa hết thông tin trong ô khoanh đỏ lên trên và xóa ô khoanh đỏ đi

Tức: dồn toàn bộ nội dung của hàng phụ đó (WindowTitle/RFID/Employee/nút
icon) lên chung 1 hàng với header chính của `Main` (logo, tên app, 2 tab,
minimize/maximize/close), rồi xóa hẳn hàng phụ.

### Đã làm

- **`Main.xaml`**: gộp lại thành **1 hàng header 44px duy nhất** (bỏ hàng 40px
  phụ và `Grid.RowDefinitions` thứ 2 — body `ContentControl` quay lại
  `Grid.Row="1"`). Grid header giờ có 8 cột: logo → tên app tĩnh
  ("SSSW – Scan and Scale Shot Weight") → `WindowTitle` động (màu xám nhạt
  `#90A4AE` để phân biệt với tên app) → nav tabs (`*`, canh giữa) → RFID/
  Employee → nút icon (Reload/History/Hydra/Settings/Update) → Minimize/
  Maximize/Close. Toàn bộ phần từ `WindowTitle` trở về RFID/icon vẫn bind qua
  `ActiveContent.DataContext.*` như cũ, không đổi cơ chế — chỉ đổi vị trí đặt
  (cùng 1 `Grid` thay vì 2 `Border` xếp chồng).

### Quyết định / giả định

- **Giữ lại text tĩnh "SSSW – Scan and Scale Shot Weight"** thay vì xóa để
  nhường chỗ hoàn toàn cho `WindowTitle`: yêu cầu chỉ nói "đưa hết thông tin
  ... lên trên", không nói xóa branding tĩnh của `Main` — đặt cả hai cạnh
  nhau (WindowTitle tô màu nhạt hơn để phân cấp thị giác) an toàn hơn suy đoán
  xóa nhầm thứ người dùng không yêu cầu bỏ.

### Build/test

- `dotnet build SSSW/SSSW.sln` (Debug) — biên dịch C#/XAML **0 lỗi thật**;
  build báo 2 lỗi `MSB3027`/`MSB3021` (copy `SSSW.exe` vào `bin/` thất bại) vì
  tiến trình `SSSW.exe` đang chạy sẵn trên máy (khoá file) — không phải lỗi
  code, cần đóng app đang chạy rồi build lại để có output sạch.
- Chưa test chạy thực tế trên máy có hardware.

## Cập nhật 2026-08-13 (tiếp #3) — Gộp dòng tiêu đề đầu tiên, đổi format WindowTitle

### Yêu cầu

Người dùng gửi 3 ảnh (tab Step Component active, tab Finished Goods active,
và 1 ảnh crop riêng dòng tiêu đề — nội dung cũ: "SSSW – Scan and Scale Shot
Weight    fVN – Shotweight Station For FG – Ver-1.0.813.1516"). Nguyên văn:

> khi chuyển qua FInished Goods thì phần header ko thay đổi, Dòng đầu tiên
> chỉnh lai thành, fVN - Scan and Scaaale Shot Weight (SSSW) - ver 123.44...

### Đã làm

- **`ShotWeightViewModel.cs`** (Step) và **`ShotWeightFGViewModel.cs`** (FG):
  đổi format chuỗi `WindowTitle` (cả giá trị field mặc định lẫn giá trị gán
  trong `InitializeAsync()`/`Application.Current.Dispatcher.Invoke(...)`) từ
  `"{loc} – Shotweight Station For Step Component – Ver-{ver}"` /
  `"{location} – Shotweight Station For FG – Ver-{ver}"` sang chung 1 pattern:
  `"{loc} – Scan and Scale Shot Weight (SSSW) – Ver-{ver}"` — bỏ hẳn phần
  "Shotweight Station For Step Component/For FG" vì 2 nút tab Step Component/
  Finished Goods trên header đã tự thể hiện đang ở màn nào, không cần lặp lại
  trong tiêu đề (đây cũng là điều làm 2 tab "trông giống hệt nhau" ở mức tổng
  thể như người dùng phản ánh — khác biệt duy nhất trước đó chỉ nằm ở cụm từ
  "For Step Component"/"For FG" giữa dòng, không nổi bật).
- **`Main.xaml`**: gộp 2 ô tiêu đề (text tĩnh "SSSW – Scan and Scale Shot
  Weight" ở cột 2 + `WindowTitle` động màu xám `#90A4AE` ở cột 3) thành
  **1 TextBlock duy nhất** ở cột 2, chỉ bind `{Binding
  ActiveContent.DataContext.WindowTitle}`, style lại thành màu trắng/13px
  (mức nổi bật của text tĩnh cũ, vì giờ là dòng tiêu đề chính duy nhất). Xoá
  1 cột khỏi `Grid.ColumnDefinitions` (8 cột → 7 cột) và renumber các cột còn
  lại lùi 1: nav tabs 4→3, RFID/Employee 5→4, nút icon 6→5, window controls
  7→6.

### Quyết định / giả định

- **Diễn giải "phần header ko thay đổi" là nhận xét bối cảnh, không phải bug
  report riêng**: đã rà lại `MainViewModel.cs`/`BaseViewModel.cs`/style
  `TitleIconBtn` — không thấy lỗi nào trong cơ chế
  `IsStepActive`/`IsFgActive`/`Visibility` ẩn hiện 3 nút Hydra/Settings/Update
  (cùng cơ chế đang chạy đúng cho phần highlight nút tab, thấy rõ trong ảnh
  người dùng gửi). Vì vậy hiểu câu này là ý "2 tab trông không khác biệt gì ở
  dòng tiêu đề" — dẫn tới yêu cầu chính là đổi format dòng đầu — chứ không
  phải báo lỗi 3 nút icon không ẩn đúng. **Chưa xác nhận lại với người dùng**;
  nếu sau này người dùng phản hồi cụ thể hơn về 3 nút icon thì cần xem lại.
- **Định dạng version giữ nguyên `Ver-{version}`** (không đổi thành literal
  `"ver 123.44"` như trong tin nhắn) — đọc câu gốc là ví dụ minh hoạ định dạng
  mong muốn ("ver X.Y...") chứ không phải version cụ thể cần hard-code; version
  thật vẫn lấy động từ `Assembly.GetExecutingAssembly().GetName().Version`
  như code cũ.

### Build/test

- Xác nhận không có tiến trình `SSSW.exe` nào đang chạy (`tasklist`) trước khi
  build.
- `dotnet build SSSW/SSSW.sln` (Debug) — **build thành công, 0 lỗi**, 754
  warning (không đổi, toàn bộ pre-existing, không liên quan thay đổi lần này).
- Chưa test chạy thực tế: chưa xác nhận trực quan dòng tiêu đề hiển thị đúng
  format mới trên cả 2 tab, và các cột còn lại (nav/RFID/icon/window controls)
  vẫn thẳng hàng đúng vị trí sau khi renumber.

## Cập nhật 2026-08-13 (tiếp #4) — Title cố định 1 lần ở Main; thêm nút nhập tay RFID

### Yêu cầu

Người dùng chạy thử bản build ở tiếp #3, gửi 2 ảnh (tab Step, tab FG): trên
tab Step dòng tiêu đề hiện đúng `"fVN – Scan and Scale Shot Weight (SSSW) –
Ver-1.0.813.1609"`, nhưng trên tab FG dòng tiêu đề lại hiện `"SSSW – Scan and
Scale Shot Weight"` (giá trị mặc định, chưa tính xong) — 2 tab hiện 2 giá trị
khác nhau. Nguyên văn:

> khi chuyển sang cân Finish Goods thì vẫn giữ nguyên Header, tuyệt đối không
> thay đổi header khi chuyển giữa 2 nội dung cân
> - thêm phần nhập tay thể RFID như trước vào

### Đã làm

- **Nguyên nhân gốc**: dòng tiêu đề bind qua
  `{Binding ActiveContent.DataContext.WindowTitle}` — mỗi ViewModel
  (`ShotWeightViewModel`/`ShotWeightFGViewModel`) tự tính `WindowTitle` bất
  đồng bộ bên trong `InitializeAsync()`/`LoadDataAsync()` (gọi DB qua
  `sp_MaterialGetCompanyName`), tốc độ khác nhau tuỳ tab và tuỳ thời điểm
  UserControl được `Loaded` lần đầu → 2 tab có thể hiện 2 giá trị khác nhau
  tại cùng 1 thời điểm, hoặc "nhảy" giá trị khi vừa chuyển tab.
- **`MainViewModel.cs`**: thêm property `WindowTitle` (mặc định
  `"SSSW – Scan and Scale Shot Weight"`) + method `LoadWindowTitleAsync()`
  tính **1 lần duy nhất** ở cấp Main (constructor gọi fire-and-forget
  `_ = LoadWindowTitleAsync();`), dùng đúng logic tra `mesocomp` →
  `EnumLocation` + `Assembly.GetExecutingAssembly().GetName().Version` giống
  hệt 2 ViewModel kia. Constructor nhận thêm
  `IDbContextFactory<DbContextDogeWH> dbFactory` (đã đăng ký sẵn trong DI ở
  `Program.cs`, không cần sửa gì thêm ở đó). Bọc `try/catch` nuốt lỗi DB lúc
  khởi động — giữ nguyên title mặc định thay vì crash app nếu DB tạm thời
  không sẵn sàng.
- **`Main.xaml`**: đổi binding dòng tiêu đề từ
  `{Binding ActiveContent.DataContext.WindowTitle}` sang
  `{Binding WindowTitle}` (property mới của `MainViewModel`, `Main.DataContext`
  vốn đã là `MainViewModel`) — giờ hoàn toàn không phụ thuộc tab nào đang
  active, tuyệt đối không đổi khi chuyển Step/FG.
- **Thêm nút "nhập tay RFID"**: thêm 1 `Button` icon (bút chì, `&#xE70F;`,
  style `TitleIconBtn` dùng lại) ngay cạnh tên nhân viên trong khu RFID/
  Employee, tooltip "Nhập tay RFID / Employee ID". Handler `btnManualRfid_Click`
  gọi chung 1 method `OpenManualRfidDialog()` (tách ra từ logic pattern-matching
  cũ trong `_tbEmployee_MouseLeftButtonDown`) — mở đúng `frmRfidInput` của
  ViewModel đang active (Step hay FG), y hệt hành vi bấm vào tên nhân viên.

### Quyết định / giả định

- **"thêm phần nhập tay thẻ RFID như trước vào"**: đã rà soát kỹ — chức năng
  nhập tay RFID qua dialog `frmRfidInput` (mở khi bấm tên nhân viên,
  `OpenRfidInputDialog()`) **vẫn tồn tại và hoạt động đúng** ở cả 2 tab, không
  hề bị mất trong các lần refactor trước; hàng "RFID + Barcode row" có ô nhập
  tay `RfidCardCode` inline trong body cũng đã bị `Visibility="Collapsed"` sẵn
  từ trước session này (không phải do lần refactor nào của session gây ra).
  Vì vậy hiểu yêu cầu là: chức năng vẫn đúng nhưng **không đủ rõ ràng/dễ thấy**
  để operator biết mà dùng (chỉ là bấm vào dòng text tên nhân viên, không có
  affordance rõ ràng) — nên thêm hẳn 1 nút icon riêng cho hành động này thay
  vì chỉnh sửa logic nào khác. **Chưa xác nhận lại với người dùng** — nếu ý
  thực sự khác (VD: muốn khôi phục lại ô `TextBox` nhập RFID hiện luôn trên
  màn hình thay vì dialog popup) thì cần điều chỉnh tiếp.
- **Không xoá property `WindowTitle` trên `ShotWeightViewModel`/
  `ShotWeightFGViewModel`**: từ giờ không còn được `Main.xaml` bind tới nữa
  (unused theo UI), nhưng để nguyên vì đây là state nội bộ vô hại của từng
  ViewModel, xoá không nằm trong phạm vi yêu cầu và có thể có chỗ khác ngầm
  phụ thuộc mà chưa rà hết.

### Build/test

- Xác nhận có tiến trình `SSSW.exe` (PID 46512) đang chạy trên máy dev lúc
  build — build C#/XAML **0 lỗi thật**, chỉ báo lại đúng 2 lỗi quen thuộc
  `MSB3027`/`MSB3021` (copy `SSSW.exe` thất bại vì bị khoá bởi tiến trình đang
  chạy) — cần đóng app đang chạy trước khi build lại để có `SSSW.exe` mới.
- Chưa test chạy thực tế: chưa xác nhận trực quan dòng tiêu đề giữ nguyên khi
  chuyển tab, và nút "nhập tay RFID" mới mở đúng dialog cho tab đang active.

## Cập nhật 2026-08-13 (tiếp #5) — Bỏ nút bút chì, quay lại bấm label để mở dialog

### Yêu cầu

Người dùng gửi ảnh khoanh đỏ + gạch chéo (X) ngay trên nút icon bút chì vừa
thêm ở tiếp #4, mũi tên chỉ vào cụm label "RFID - Employee ID: 07387 - Nguyen
Dinh Cong". Nguyên văn:

> click vào label hiển thị số thẻ và tên thì mở form nhâp RFID, xóa cái iden
> cây viết đi

Tức: không cần nút icon riêng — chỉ cần bấm vào label hiển thị số thẻ/tên
nhân viên là mở được dialog nhập tay RFID (đúng như hành vi gốc đã có sẵn từ
trước tiếp #4); xoá hẳn nút bút chì vừa thêm.

### Đã làm

- **`Main.xaml`**: xoá `Button` icon bút chì (`&#xE70F;`, `btnManualRfid_Click`)
  thêm ở tiếp #4. Giữ nguyên `TextBlock` `_tbEmployee`
  (`MouseLeftButtonDown="_tbEmployee_MouseLeftButtonDown"`, `Cursor="Hand"`) —
  đây vốn đã là cơ chế "bấm label mở dialog" người dùng yêu cầu, chỉ trả lại
  `Margin` ban đầu (10,0) sau khi bỏ nút cạnh nó.
- **`Main.xaml.cs`**: xoá `btnManualRfid_Click` và method trung gian
  `OpenManualRfidDialog()`; gộp lại logic pattern-matching thẳng vào
  `_tbEmployee_MouseLeftButtonDown` như bản gốc trước tiếp #4.

### Quyết định / giả định

- Không có giả định mới — yêu cầu lần này rõ ràng, chỉ là revert đúng phần đã
  thêm sai ý ở tiếp #4 (nút bút chì) trong khi giữ nguyên fix chính (title cố
  định 1 lần ở `MainViewModel`, không phụ thuộc tab) — 2 việc độc lập nhau,
  không việc nào ảnh hưởng việc kia.

### Build/test

- Xác nhận không có tiến trình `SSSW.exe` nào đang chạy trước khi build.
- `dotnet build SSSW/SSSW.sln` (Debug) — **build thành công, 0 lỗi**, exe copy
  thành công (không bị khoá file lần này), 754 warning pre-existing.
- Chưa test chạy thực tế trên máy có hardware.

## Cập nhật 2026-08-13 (tiếp #6) — Dọn code trùng WindowTitle ở 2 ViewModel con

### Yêu cầu

Người dùng dán lại đúng đoạn code tính `location`/`appVersion`/`WindowTitle`
bên trong `ShotWeightViewModel.InitializeAsync()` (đoạn cũ, giờ đã dead code
vì `Main.xaml` không còn bind tới `ActiveContent.DataContext.WindowTitle`
nữa từ tiếp #4). Nguyên văn:

> đưa phần hiển thị này ra form main để hiển thị lên header
> click _tbEmployee vào textblock này thì show form để nhập rfid bằng tay

Hiểu là: (1) dọn hẳn đoạn tính toán trùng lặp này ra khỏi
`ShotWeightViewModel`/`ShotWeightFGViewModel` — vì đã "đưa ra Main" thật sự
(tính trong `MainViewModel.LoadWindowTitleAsync()`) từ tiếp #4, chỉ chưa dọn
code cũ còn sót lại 2 nơi kia; (2) xác nhận lại hành vi bấm `_tbEmployee` mở
form nhập tay RFID — hành vi này đã đúng như yêu cầu sẵn từ tiếp #5, không
cần sửa gì thêm.

### Đã làm

- **`ShotWeightViewModel.cs`**: xoá đoạn tính `location`/`appVersion` +
  `WindowTitle = ...` trong `InitializeAsync()` (giữ nguyên `_mesocomp`/
  `_mesoYear` vì 2 field này vẫn được dùng ở chỗ khác — gán vào
  `Mesocomp = _mesocomp` trong `FT600`/BOM lookup). Xoá luôn property
  `WindowTitle` + field `_windowTitle` (không còn ai đọc/ghi).
- **`ShotWeightFGViewModel.cs`**: xoá tương tự đoạn `location`/`appVersion` +
  `WindowTitle = ...` trong khối `Application.Current.Dispatcher.Invoke(...)`
  của phần load FG data, và property/field `WindowTitle`/`_windowTitle`.
- **`_tbEmployee` → mở form nhập tay RFID**: không đổi gì — đã đúng hành vi
  yêu cầu từ tiếp #5 (`Main.xaml`'s `_tbEmployee` TextBlock,
  `MouseLeftButtonDown="_tbEmployee_MouseLeftButtonDown"` → pattern-match gọi
  `OpenRfidInputDialog()` của ViewModel đang active).

### Quyết định / giả định

- **Không xoá `_mesocomp`/`_mesoYear` khỏi 2 ViewModel**: dù không còn dùng để
  tính `WindowTitle` nữa, 2 field này vẫn phục vụ mục đích khác (gán
  `Mesocomp` vào entity khi ghi `FT600`) — đã grep xác nhận trước khi xoá, chỉ
  xoá đúng phần code compute-and-display `WindowTitle` không còn ai tiêu thụ.

### Build/test

- Xác nhận không có tiến trình `SSSW.exe` nào đang chạy trước khi build.
- `dotnet build SSSW/SSSW.sln` (Debug) — **build thành công, 0 lỗi**, 754
  warning pre-existing (không đổi).
- Chưa test chạy thực tế trên máy có hardware.

## Cập nhật 2026-08-13 (tiếp #7) — Fix crash khi khởi động: `InvalidOperationException` ở `MainViewModel.LoadWindowTitleAsync()`

### Yêu cầu

Người dùng báo lỗi runtime khi khởi động app (kèm ảnh chụp Visual Studio
"Exception Caught"):

> lỗi khi khởi động

```
System.InvalidOperationException: 'FromSql' or 'SqlQuery' was called with
non-composable SQL and with a query composing over it. Consider calling
'AsEnumerable' after the method to perform the composition on the client side.
```

Bắt tại `MainViewModel.LoadWindowTitleAsync()`, dòng gọi
`db.Database.SqlQueryRaw<string>("sp_MaterialGetCompanyName").FirstOrDefaultAsync()`.

### Nguyên nhân

`sp_MaterialGetCompanyName` là stored procedure (`EXEC`), không phải câu
`SELECT` thuần — không composable. `.FirstOrDefaultAsync()` khiến EF Core cố
compose thêm `TOP(1)` lên trên, ném exception lúc chạy (không phải lỗi biên
dịch nên build vẫn pass, chỉ crash khi thực thi). Đây là hệ quả trực tiếp của
lần fix `CS0411` ở tiếp #4/#5 (đã lỡ bỏ `.AsEnumerable()` khi chain
`.FirstOrDefaultAsync()` cho gọn code) — lúc đó chỉ kiểm tra hết lỗi biên
dịch, chưa chạy thử thực tế nên không phát hiện ra.

Đối chiếu với các chỗ gọi `sp_MaterialGetCompanyName` khác trong code (đều
đang chạy đúng, không lỗi):
`ShotWeightViewModel.cs:545`, `ShotWeightFGViewModel.cs:444`,
`frmShotWeightScale.cs:628`, `frmShotWeightScaleV2.cs:239` — tất cả đều dùng
`.AsEnumerable().FirstOrDefault()` (client-eval, không compose), xác nhận đây
đúng là pattern chuẩn cho stored procedure không composable trong codebase
này.

### Đã làm

- **`MainViewModel.cs`** — `LoadWindowTitleAsync()`: đổi
  `.FirstOrDefaultAsync()` (compose trên `IQueryable`, gây crash) thành
  `.AsEnumerable().FirstOrDefault()` (client-eval, không compose), bọc trong
  `await Task.Run(() => ...)` để giữ nguyên tính async của method (không
  block UI thread khi khởi động).

### Build/test

- `dotnet build SSSW/SSSW.sln` — **build thành công, 0 lỗi**, 754 warning
  (không đổi).
- Chưa test chạy thực tế trên máy có DB — cần người dùng xác nhận app khởi
  động không còn crash.
