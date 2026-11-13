# 2026-11-12 — Ghép child BOM Level 2 theo mã chính (bỏ màu & size)

## 1. Yêu cầu & Bối cảnh
`sp_getBomWinlineOfItemFG @itemFG = <step>` trả child ở Level 2 chỉ có mã chính (`1-VB042313`), trong khi step Level 1 / FT600 có mã đầy đủ (`1-VB042313-A123-5065`). Khớp chuỗi chính xác nên child luôn "chưa cân", C039 = 0, chain/cascade sai. Ngoại lệ: mã `REX…` giữ nguyên.

## 2. Phân tích
Các chỗ so khớp child ↔ step: `IsSelfRow`, `ComponentQuantityAggregator`, `PiecesFor`/`_pieceQtyByCode`, `FindWeighedChild`, `GetLatestByStepCodesAsync`, `IsChainOnly`, `ChainPredecessorCodes`, `CascadeLevel2`, `RefreshLevel2Details`.

## 3. Thay đổi
- `PartWeightCalculator`: thêm `StepMainCode` (2 phần đầu; REX giữ nguyên) và `SameStep`; `IsSelfRow` so theo mã chính (vẫn giữ luật `-0000` / đơn vị `G`).
- `ComponentQuantityAggregator`: khóa gộp C039 theo mã chính.
- `ScaleStepRepository.GetLatestByStepCodesAsync`: REX khớp nguyên mã, còn lại `C002 StartsWith 'N-VB042313-'`; trả bản mới nhất theo từng mã đầy đủ.
- `ShotWeightViewModel`: `FindWeighedChild(code, parent)` khớp theo mã chính, nhiều dòng thì **ưu tiên cùng size với step cha (C008)**, không có thì dòng đầu (phiên hiện tại trước, rồi bản đã lưu mới nhất); chain/cascade/Row Details so theo mã chính.
- Test: `StepMainCodeTests.cs` mới; cập nhật 2 test cũ của aggregator sang khóa mã chính.

## 4. Quyết định / Giả định
- Hướng A (đã được duyệt) khi một mã chính khớp nhiều size.
- C039 gộp theo mã chính, tính trên mọi size.
- Dòng self giữ nguyên luật nhận diện.

## 5. Kết quả
`dotnet test tests/SSSW.Tests`: 17/17 pass. Chưa kiểm thử trên dữ liệu DB thật trên máy sản xuất.
