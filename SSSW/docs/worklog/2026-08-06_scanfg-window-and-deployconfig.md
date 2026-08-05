# 2026-08-06 — Nút "Scan FG" + cửa sổ ShotWeightFGWindow, tách DeployConfig.props

## Yêu cầu

1. Thêm nút mới trên title bar của `ShotWeightWindow` (icon hình cái cân), bấm vào
   mở một cửa sổ WPF mới — giao diện tương tự `ShotWeightWindow` nhưng bỏ hết dàn
   nút title bar "từ Update trở đi", đổi "TOTAL STEPS" → "DANH SÁCH CÂN MẪU".
   Yêu cầu ban đầu: **chỉ làm giao diện, code (logic/ViewModel) làm sau**.
2. Sau khi xem ảnh chụp: bỏ 4 nút Reload/History/Hydra/Settings khỏi cửa sổ mới,
   giữ lại Minimize/Maximize/Close.
3. Icon nút "Scan FG" bị chìm (màu tối trên nền title bar tối) → cần nền sáng để
   thấy nút; đồng thời nối command để bấm vào **thực sự mở** cửa sổ mới lên.
4. Cửa sổ mới khi mở bị "treo" ở màn hình Loading vĩnh viễn → sửa lỗi.
5. Đổi tên toàn bộ `ScanFGWindow`/`ScanFG` → `ShotWeightFGWindow`/`ShotWeightFG`.
6. Tách đường dẫn share deploy (`DeployDir` trong `.csproj`) ra file cấu hình
   riêng để đổi site (fFT/fVN) không cần sửa `.csproj`.

## Đã làm

- **`SSSW/UI/WPF/ShotWeightWindow.xaml`**: thêm nút title bar mới, `Command="{Binding ScaneFGCommand}"`,
  `Background="White"` (icon `scale_30.png` vốn tối màu, cần nền sáng mới thấy trên
  title bar `#0D1117`), đặt trước cụm nút Reload/History/Hydra/Settings/Update/Minimize/Maximize/Close.
- **`SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs`**: thêm `public RelayCommand ScaneFGCommand`,
  wire trong constructor, thêm method `OpenShotWeightFGWindow()` (resolve
  `ShotWeightFGWindow` qua `_serviceProvider.GetRequiredService<T>()`, set `Owner`,
  `ShowDialog()`).
- **`SSSW/UI/WPF/ShotWeightFGWindow.xaml` + `.xaml.cs`** (file mới, tạo từ bản copy
  của `ShotWeightWindow.xaml`): title bar chỉ còn Minimize/Maximize/Close; header
  "DANH SÁCH CÂN MẪU"; code-behind chỉ có handler tối thiểu (đóng/thu nhỏ/phóng to
  + các stub trống để XAML compile được, chưa nối ViewModel).
- **`SSSW/Program.cs`**: đăng ký `services.AddTransient<ShotWeightFGWindow>();`.
- **`SSSW/SSSW.csproj`**: đăng ký `scale_30.png`/`framas_mini_white.png` làm WPF
  `<Resource>` (khác với WinForms `Resources.resx` — hai cơ chế resource không
  tự dùng chung được, xem CLAUDE.md).
- **`SSSW/DeployConfig.props`** (file mới): chứa `PropertyGroup` với `DeployDir`
  (fFT active, fVN comment sẵn).
- **`SSSW/SSSW.csproj`**: thay khối `DeployDir` inline bằng `<Import Project="DeployConfig.props" />`.
  Các target `GenerateUpdateXml`/`CreateUpdateZip`/`CopyUpdateToShare` (chỉ chạy khi
  build Release) không đổi, vẫn dùng `$(DeployDir)` (nay đến từ file import).

## Quyết định / giả định

- Tên "ShotWeightFGWindown" trong yêu cầu người dùng được hiểu là lỗi gõ của
  **"ShotWeightFGWindow"** (dựa vào câu thứ 2 cùng message: "ScanFG → ShotWeightFG").
  **Chưa được người dùng xác nhận lại.**
- `ScaneFGCommand` (thừa chữ "e") **cố ý không đổi tên** — yêu cầu rename chỉ nói
  "ScanFG" → "ShotWeightFG", không nhắc đến "Scane" → "Scan". Đã báo lại cho người
  dùng, chưa có quyết định cuối.
- `DeployConfig.props` để **git-tracked bình thường** (không gitignore) — vì CLAUDE.md
  mô tả site targeting là cấu hình cấp-repo ("this repo currently targets the fFT
  site"), không phải preference riêng từng máy dev. Đã báo lại cho người dùng,
  chưa có xác nhận cuối cùng — nếu mỗi máy dev cần trỏ site khác nhau thì nên
  đổi sang file gitignore + `.props.example` mẫu thay vì file tracked này.

## Việc còn mở

- Xác nhận tên đúng: `ShotWeightFGWindow` hay đúng là muốn gõ khác.
- Có đổi `ScaneFGCommand` → `ShotWeightFGCommand` không.
- Có giữ `DeployConfig.props` là file tracked hay chuyển sang gitignored per-machine không.
- Phần logic (ViewModel/DataContext/DB) cho `ShotWeightFGWindow` **chưa làm** — theo
  đúng yêu cầu ban đầu "tập trung làm giao diện, code sau". Hiện cửa sổ mở/đóng
  được nhưng không có DataContext, các control (grid, lookup, textbox cân) chưa
  bind vào logic thật.
- Chưa test build Release để xác nhận pipeline `GenerateUpdateXml`/`CreateUpdateZip`/
  `CopyUpdateToShare` vẫn hoạt động đúng với `DeployDir` lấy từ file import (chỉ
  mới build Debug để xác nhận `<Import>` parse được).

## Build/test

- `dotnet build SSSW/SSSW.csproj -c Debug` — build sạch (0 error) sau khi xóa
  `obj`/`bin` để loại bỏ cache `.g.cs` cũ (lỗi `CS2001` do WPF XAML codegen cache
  lệch, không phải lỗi code thật).
- Lần build cuối (sau khi thêm `DeployConfig.props`) có lỗi `MSB3027`/`MSB3021`
  (copy `apphost.exe` → `SSSW.exe` thất bại) — **không phải lỗi biên dịch**, do
  app đang chạy sẵn trên máy khóa file `.exe`. Đã lọc riêng `CS`/`MSB` error khác
  → không có lỗi nào khác ngoài lỗi khóa file này.
