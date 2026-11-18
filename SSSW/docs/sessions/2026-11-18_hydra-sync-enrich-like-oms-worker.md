# 2026-11-18 — HydraCommand: bổ sung & lọc dữ liệu Hydra như OMS WorkerShotWeight

## 1. Yêu cầu & Bối cảnh
Tham khảo `OMS.KerryAPIServices.Worker/Workers/WorkerShotWeight.cs` (phần `GetDataHydra`) để cập nhật cách insert FT601 của nút Hydra (`MainViewModel.GetDataHydraAsync`).

## 2. Phân tích
Worker, trước khi insert FT601: (1) gọi `sp_sssw_getproductInfo` (lô 100 mã) bổ sung các ô trống (FG, tên, step index, main code/name, size); (2) loại dòng không có FG hợp lệ (chứa `-`); (3) loại dòng vẫn thiếu Main code / Size, ghi log cảnh báo FG thiếu thông tin; (4) insert với `CreatedBy = machine`. HydraCommand cũ insert thẳng kết quả `sp_GetFullStepItemHydraIsRun`, và `HydraItemDetailModel.Size` là `[NotMapped]` nên `C002` (size) luôn rỗng.

## 3. Thay đổi
- `models/ProductInfoStoreModel.cs` (mới): DTO của `sp_sssw_getproductInfo`.
- `UI/WPF/Services/HydraItemEnricher.cs` (mới, hàm thuần): `NeedsInfo`, `LookupCodes`, `FillMissingInfo` (chỉ điền ô trống, không ghi đè), `KeepWithFg`, `FgMissingInfo`, `KeepComplete`.
- `MainViewModel.GetDataHydraAsync`: timeout 200s; gọi `FillMissingHydraInfoAsync` (lỗi chỉ log cảnh báo); lọc theo các bước trên; `CreatedBy = machine`. Phần so khớp trùng FT601 (mã step + máy + lệnh Hydra) và mapping cột giữ nguyên.
- Test: `HydraItemEnricherTests.cs` (5 test).

## 3b. Bổ sung — loading ngay + thông báo kết quả
- `Main.xaml` + `MainViewModel` (`IsOverlayVisible`, `OverlayMessage`): lớp phủ chờ cấp app hiện **ngay** khi bấm nút Hydra (đợi WPF vẽ xong mới làm việc nặng), đổi nội dung theo bước (Getting data… / Completing item information… / Saving to FT601…), chặn thao tác và bỏ qua bấm lặp.
- Kiểm tra trùng FT601 chuyển từ 1 truy vấn / item (chạy trên UI thread → đơ) sang 1 truy vấn lấy các khóa (mã step + máy + lệnh Hydra) đã có rồi so trong bộ nhớ; trùng ngay trong đợt cũng không insert lại.
- Xong thì ẩn lớp phủ và hiện thông báo: số item lấy từ Hydra, số item insert vào FT601 (kèm số đã có sẵn / số bị bỏ do thiếu FG-Main-Size nếu > 0). Lỗi thì hiện hộp thoại lỗi (trước đây chỉ ghi log). `MainViewModel` nhận thêm `IDialogService`.

## 4. Quyết định / Giả định
- Không port `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` (trên connection EF được mở/đóng theo lệnh nên không giữ hiệu lực ổn định).
- Hệ quả cần biết: dòng Hydra thiếu Main code / Size sau khi bổ sung sẽ **không** được insert vào FT601 nữa (giống worker).

## 5. Kết quả
`dotnet test tests/SSSW.Tests`: 33/33 pass. Chưa chạy nút Hydra trên DB thật.
