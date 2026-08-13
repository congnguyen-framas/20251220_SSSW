# Worklog Index

Nhật ký các phiên làm việc trên codebase SSSW. Mỗi phiên (hoặc mỗi task đáng kể)
có **một file riêng** trong thư mục này (`docs/worklog/`), được liệt kê tại đây
theo thứ tự **mới nhất lên trên**.

Mục đích: khi bắt đầu một phiên làm việc mới (kể cả với Claude Code), đọc file
mới nhất (hoặc vài file gần nhất liên quan đến task đang làm) để nắm nhanh
trạng thái/quyết định gần đây, **không cần đọc lại toàn bộ code hoặc git log**.

Xem hướng dẫn quy ước tại cuối file này trước khi thêm entry mới.

## Danh sách phiên làm việc

| Ngày       | File                                                                     | Tóm tắt                                                                                  |
| ---------- | ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| 2026-08-13 | [2026-08-13_fix-default-step-tab-startup-race.md](2026-08-13_fix-default-step-tab-startup-race.md) | Fix mở app lên đôi lúc **không** hiện mặc định tab Step Component (body trống, không tab nào được check dù title vẫn load đúng) — race condition không ổn định, xác nhận qua đối chiếu `Logs/SSSW_Log_*.txt` thật: `MainViewModel`'s constructor tự bắn `_ = StartupAsync()` NGAY TRONG lúc DI resolve `Main` ở `Program.cs`, tức TRƯỚC dòng `wpfApp.Run(wpfWin)` khởi động Dispatcher message pump; vì query Config/mesocomp trả lời rất nhanh (vài ms trên LAN), `StartupAsync()` (gồm cả `SelectStep()` gắn `ShotWeightWindow` vào `ActiveContent`) có thể chạy xong TRƯỚC KHI cửa sổ kịp `Show()`, khiến `ShotWeightWindow.Loaded`/`OnLoaded` không bắn ở lần gắn đầu. Fix: dời lệnh gọi `StartupAsync()` ra khỏi constructor `MainViewModel`, chuyển vào `Main_Loaded` (subscribe `Loaded` trong constructor `Main.xaml.cs`) — đảm bảo chỉ chạy sau khi window đã thực sự `Loaded`; đổi `StartupAsync()` từ `private` sang `public`; thêm `_logger.LogError` vào `catch` (trước đây rỗng, nuốt exception im lặng) và bọc thêm try/catch quanh `SelectStep()` trong `finally` (trước đây có thể trở thành Unobserved Task Exception vô hình). |
| 2026-08-13 | [2026-08-13_fix-rfid-input-click-and-unlock-icon-buttons.md](2026-08-13_fix-rfid-input-click-and-unlock-icon-buttons.md) | Fix `_tbEmployee` (label "RFID - Employee ID") không mở dialog nhập tay RFID được: `TextBlock` không có `Background` chỉ hit-test vùng glyph, khi `UserName` rỗng (chưa scan RFID) không có glyph nào nên click lọt xuống `Border` cha (trigger kéo-thả cửa sổ) — thêm `Background="Transparent"` + `MinWidth="24"` cho `_tbEmployee`, thêm `e.Handled = true` để chặn bubble kéo-thả sau khi dialog đóng. Fix nút Hydra/Settings/Update "khoá" (không tác dụng) khi ở tab FG: 3 command này trước đây chỉ tồn tại trên `ShotWeightViewModel`, bind `ActiveContent.DataContext.HydraCommand` resolve null khi tab FG active — chuyển hẳn 3 command + AutoUpdater plumbing từ `ShotWeightViewModel` lên `MainViewModel` (hành động cấp app dùng chung Step/FG), rebind trong `Main.xaml` sang `{Binding HydraCommand}` (không qua `ActiveContent`), thêm `RefreshActiveContent()` để refresh đúng tab đang active sau Hydra sync/Settings đóng. |
| 2026-08-13 | [2026-08-13_rfid-connect-to-main-global-var.md](2026-08-13_rfid-connect-to-main-global-var.md) | Chuyển việc load Config (`GlobalVariable.ConfigSystem`) + kết nối hardware (Barcode/RFID/Scale qua `DeviceConnectionService`) từ `ShotWeightViewModel.InitializeAsync()` lên `MainViewModel.StartupAsync()` — chạy 1 lần ở cấp Main trước khi tab Step mặc định được mở (dời `SelectStep()` xuống `finally` của `StartupAsync()`). Thêm biến toàn cục `GlobalVariable.Devices` (static, gán bởi Main) để `ShotWeightWindow`/`ShotWeightFGWindow` đọc `DeviceConnectionService` thay vì nhận qua constructor DI; xóa `ConnectHardwareAction` delegate (không còn cần thiết). |
| 2026-08-13 | [2026-08-13_main-shell-step-fg-tabs.md](2026-08-13_main-shell-step-fg-tabs.md) | Thêm `Main.xaml` (shell window với 2 tab Step Component / Finished Goods); chuyển `ShotWeightWindow`/`ShotWeightFGWindow` từ `Window` sang `UserControl`; bỏ cơ chế modal dialog "Scan FG". Cập nhật tiếp: gộp header (WindowTitle/RFID/nút icon) của từng view lên header phụ trên `Main.xaml`, bind động qua `ActiveContent.DataContext.*`; sau đó gộp tiếp hàng header phụ vào chung 1 hàng header duy nhất với chrome của `Main`; cuối cùng gộp dòng tiêu đề đầu tiên thành 1 dòng duy nhất và đổi format `WindowTitle` thành `"{Location} – Scan and Scale Shot Weight (SSSW) – Ver-{version}"` (bỏ "Shotweight Station For Step Component/FG"); sau đó chuyển việc tính `WindowTitle` lên `MainViewModel` (tính 1 lần duy nhất, không phụ thuộc tab active) để header tuyệt đối không đổi khi chuyển Step/FG; thêm rồi bỏ lại nút icon "nhập tay RFID" — giữ nguyên cơ chế bấm label số thẻ/tên nhân viên để mở dialog nhập tay RFID; cuối cùng dọn nốt code `WindowTitle`/`location`/`appVersion` còn sót lại (dead code) ở `ShotWeightViewModel`/`ShotWeightFGViewModel` — `MainViewModel` giờ là chủ sở hữu duy nhất của header title; fix crash khởi động `InvalidOperationException` (non-composable SQL) do `.FirstOrDefaultAsync()` trên stored procedure ở `MainViewModel.LoadWindowTitleAsync()` — đổi lại thành `.AsEnumerable().FirstOrDefault()`. |

---

## Quy ước khi thêm entry mới

1. **Tên file**: `YYYY-MM-DD_slug-ngan-gon.md` (slug tiếng Anh, không dấu, nối bằng `-`).
   Nếu cùng ngày có nhiều task không liên quan, tách thành nhiều file (không dồn chung).
2. **Nội dung mỗi file** nên có các mục:
   - `## Yêu cầu` — người dùng yêu cầu gì (nguyên văn hoặc diễn giải ngắn).
   - `## Đã làm` — thay đổi cụ thể, kèm đường dẫn file (`SSSW/...`).
   - `## Quyết định / giả định` — chỗ nào tự suy đoán ý người dùng, đặt tên, chọn giải pháp khi có nhiều lựa chọn.
   - `## Việc còn mở` — câu hỏi chưa chốt, việc cố ý để lại cho phiên sau ("code sau", v.v.).
   - `## Build/test` — kết quả build cuối cùng (pass/fail, lỗi nào là thật vs. nhiễu môi trường).
3. Sau khi thêm file mới, **cập nhật bảng ở trên** (dòng mới lên đầu bảng, ngay dưới header).
4. File này (`INDEX.md`) và các file trong `worklog/` được **commit vào git bình thường** — không gitignore.
5. Không sửa lại nội dung các entry cũ trừ khi thông tin trong đó sai — worklog là nhật ký, không phải tài liệu sống (living doc); tài liệu sống là `ShotWeightWindow_Documentation_EN.md` và `CLAUDE.md`.
