# 2026-11-17 — PU: xác định "step cuối" theo BOM, không theo FT601.C010

## 1. Yêu cầu & Bối cảnh
FG PU `6813992601-SG10-F00L` (QR step 4 = step cuối): grid Total Steps chỉ còn step 3 và 4, mất step 1 (Logo) và 2 (Outsole); Active nhảy sang step 3. FG `…-F05L` cùng loại thì đủ 4 step.

## 2. Phân tích
`GetDataAsync` có nhánh dọn dẹp PU chỉ nên chạy khi step chọn **không phải step cuối**: nó xóa mọi step trừ step đang cân và step chain ngay trước. Điều kiện cũ so `FT601.C010` (số thứ tự từ Hydra) với max `ParallelSequence` của BOM. Hai số khác nguồn nên có thể lệch → step cuối bị coi là không cuối → mất step 1/2 và Outsole (child Level 2 của step 4).

## 3. Thay đổi
- `Services/StepSequence.cs` (mới): `IsNotLastStep` lấy thứ tự của step theo mã trong BOM Level 1; chỉ khi mã không có trong BOM mới dùng `FT601.C010` làm dự phòng. REX không tính vào step cuối.
- `ShotWeightViewModel.GetDataAsync`: dùng `StepSequence.IsNotLastStep`; sửa comment cũ ("KHÔNG xóa các step khác") cho đúng hành vi.
- Test: `StepSequenceTests.cs` (4 test).

## 3b. Bổ sung — nhiều step chain cùng thứ tự đều là previous step
`ChainPredecessors` trước đây chỉ lấy 1 step khi có nhiều step cùng Step No. (vd. Studs `1-VB322617-NOC1-2658` và Rim `2-VB322617-NPY7-2952`, cùng Step No. 1) → thiếu previous step khi tính Part Weight. Nay `StepSequence.PickPerComponent` nhóm theo mã chính (bỏ màu & size): khác component thì **lấy hết**, cùng component nhiều size thì lấy 1 (ưu tiên cùng size). Điều kiện chain (`IsChainOnly`) và lọc L/R của PU giữ nguyên. Test: +2 trong `StepSequenceTests`.

## 4. Giả định
Chưa xác nhận trực tiếp giá trị `FT601.C010` / `ParallelSequence` của FG F00L trên DB (không truy cập được DB); sửa theo hướng đã được duyệt vì loại bỏ hẳn nguồn lệch.

## 5. Kết quả
`dotnet test tests/SSSW.Tests`: 28/28 pass. Cần cân thử lại FG `6813992601-SG10-F00L` và FG có Studs + Rim cùng Step No. (`6112322617`) trên máy thật để kiểm chứng.
