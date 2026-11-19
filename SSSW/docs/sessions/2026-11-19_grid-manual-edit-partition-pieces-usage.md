# 2026-11-19 — Sửa tay trực tiếp trên lưới TOTAL STEPS: Actual Partitioning / Pieces Qty / Percent of Use

## 1. Yêu cầu & Bối cảnh
FG `6112042509-C562-3101`: step Studs ép 22 chiếc/đôi nhưng các step sau chỉ dùng một phần (12 + 6 + …), nên tổng Pieces Qty (C039) cộng theo BOM Level 2 không đúng. Yêu cầu: vẫn tính tổng theo Level 2 làm giá trị mặc định, nhưng cho **sửa tay** ngay trên ô lưới, tô nổi ô để biết sửa được, và khi sửa thì cập nhật mọi thông tin liên quan. Áp dụng cho cột **Actual Partitioning**, **Pieces Qty**, **Percent of Usage**. Actual Partitioning vẫn sửa được ở card Step Information.

## 2. Thay đổi
- `ShotWeightWindow.xaml`: 3 cột chuyển sang `DataGridTemplateColumn` có `TextBox` (style `EditableGridCellBox` trong `ShotWeightStyles.xaml`: nền vàng nhạt, viền cam, tooltip). Actual Partitioning / Pieces Qty có nhãn đơn vị (prs / pcs / pcs-L / pcs-R) bên dưới (`ValueUnitConverter` thêm `unit:count`, `unit:pcs`). Percent of Usage chỉ sửa được ở dòng `UsageEditable` (REX/RPU thuộc category non-woven/mesh — cùng điều kiện ô Usage % ở card); dòng khác chỉ đọc (không viền, không focus).
- `ShotWeightWindow.xaml.cs`: Enter / rời ô → commit; Esc → hủy; commit hoãn qua `Dispatcher.BeginInvoke` vì việc tính lại dựng lại cả lưới; cờ `_cellCommitted` chặn commit lần 2 (hộp thoại cảnh báo / dựng lại lưới làm mất focus). Click vào ô focus thẳng TextBox, không làm đổi / bỏ chọn dòng (`IsInsideButtonOrDetails` bỏ qua `TextBox`).
- `ShotWeightViewModel.cs`:
  - `OnGridCellEdited(row, field, text)` ("C028" | "C039" | "C035"): kiểm tra giá trị (N, Pieces > 0; % từ 0 đến 100), cùng giá trị thì no-op; Pieces để trống + Enter = bỏ giá trị sửa tay, quay về tổng Level 2.
  - Pieces: lưu vào `_pieceOverrideByCode` (khóa theo **mã chính**, áp cho mọi size cùng mã chính; xóa ở `ResetNewLoop`); `ApplyPieceQuantities` áp override sau khi tổng hợp Level 2. `ApplyPiecesChange`: tính lại C036 = C021 / C039 của các dòng cùng mã chính (REX nạp từ lịch sử: tổng = g/chiếc × số chiếc mới) rồi cascade sang các step trừ chi tiết đó.
  - Actual Partitioning: `ReapplyWeights(row)` — tính lại Runner, Part Weight (Level 2 / công thức cũ / Logo-Cleat_Ring = C023 / N), Weight per Unit, phân bổ 1 khuôn nhiều size, cascade. Step chưa cân đủ thì chỉ đổi N. Card Step Information (`OnActualPairsEnter`) cũng gọi `ReapplyWeights` (trước đây chỉ đổi số).
  - Percent of Usage: `ApplyUsagePct` / `ApplyUsageToRow` — Part = (Total × % / 100) / N; REX/RPU (g) chỉ đổi dòng đó, non-woven/mesh dùng chung 1 % cho mọi dòng loại đó. `OnUsagePctEnter` (card) dùng chung đường này, có cascade.
  - Tách từ `ExecuteSave` thành hàm dùng chung: `DistributeMultiSizeMold(src)`, `CascadeFrom(src)`; `CascadeLevel2(changedRow)` nhận tham số thay vì đọc `_rowSelected`. Hành vi Save giữ nguyên.
- `FT600.partial.cs`: `UsageEditable`, `UsageReadOnly` (UI only).

## 2b. Bổ sung trong cùng phiên
- **Pieces Qty mặc định 2** cho step ép không có trong BOM Level 2 của step nào (`DefaultPiecesNotInLevel2`, không áp cho REX/RPU); nhãn đơn vị cột Pieces Qty luôn "pcs".
- **Pieces Qty sửa tay theo từng dòng (size)** — khóa theo mã step đầy đủ, không lan sang size khác cùng mã chính (vd. 3 size Logo). Mục "theo mã chính" ở §2 được thay bằng quy tắc này. REX: tổng khối lượng đã cân giữ nguyên, Weight per Unit = tổng / số chiếc mới.
- **Percent of Usage**: chỉ sửa được ở REX/RPU thuộc category chỉ dùng một phần; dòng khác chỉ đọc.
- **Lỗi lưới trống khi cân Outer_Studs / Inner_Studs**: bộ lọc nhóm Studs trong `GetDataAsync` so `StartsWith("Studs")` nên xóa hết dòng "Outer_Studs…" / "Inner_Studs…" → dùng `IsStudsName`.
- **Lỗi Outer_Studs bị coi là step "chain" trong popup cân**: `IsChainOnly` chỉ xét các dòng còn trên lưới (đã lọc, mất Base dùng Outer_Studs ở Level 2) → nay xét toàn bộ step của FG theo BOM; `PreloadLevel2Async` nạp Level 2 của mọi step trong BOM.
- **Bố cục lưới**: thu hẹp 14 cột đầu để Weight per Unit hiện không cần cuộn ngang; PPIC Partitioning và Mold Pairs Shot chuyển xuống cuối lưới.

## 3. Quyết định / Giả định
- Pieces Qty sửa tay áp cho **mọi size cùng mã chính** (đúng với cách C039 được gộp theo mã chính).
- Runner = NO (C022 đang 0) giữ 0 khi đổi N; Runner = YES tính lại (Total − Total Part) / N.
- `OnUsagePctEnter` cũ không chia N khi `C035 == 100` (lệch với `ExecuteSave`); nay luôn chia N cho khớp `ExecuteSave`. Chỉ khác khi N > 1.
- Giá trị sửa tay lưu vào FT600 (C028 / C039 / C035) khi Confirm như các cột khác; không có cột riêng đánh dấu "sửa tay".

## 4. Kết quả
`dotnet test tests/SSSW.Tests`: 33/33 pass, project build. Logic sửa tay nằm trong ViewModel (không có test tự động); **chưa chạy thử trên màn hình thật** — cần kiểm tra FG `6112042509-C562-3101`: sửa Pieces Qty của Studs từ 18 thành 22 / 12 rồi xem Weight per Unit và Part Weight của các step sau.
