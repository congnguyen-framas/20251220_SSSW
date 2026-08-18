# 2026-08-18 — Tự động chuyển/xóa `_rowSelected` sau khi cân xong 1 bước (Step tab)

## Yêu cầu

> ở form cân step, hiện tại khi cân hoàn thành 2 bước thì vẫn để nguyên biến chọn
> bước cân `_rowSelected`, dẫn đến tình trạng nếu user lỡ bấm nút Save thì nó sẽ
> cập nhật lại giá trị part. Sửa lại khi cân 2 bước cho step xong, thì tự động
> chuyển biến chọn data cân sang bước tiếp theo, còn nếu không có bước tiếp theo
> thì xóa biến data chọn cân đi.

Bug cụ thể: item injection cần 2 lần bấm Save (lần 1 ghi tổng khối lượng vào
`C024`, lần 2 tính `C023`/`C022`/`C021`); REX/non-injection chỉ cần 1 lần. Sau khi
1 dòng đã cân xong (`C021 > 0`), `_rowSelected` vẫn trỏ vào dòng đó — nếu user
bấm Save thêm 1 lần nữa (nhầm), code rơi vào nhánh "lần cân thứ 2" (vì `C024 !=
0`) và ghi đè `C023`/`C021` bằng `_scaleValue` hiện tại (rác/giá trị cân khác).

## Đã làm

- [SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs](../../UI/WPF/ViewModels/ShotWeightViewModel.cs)
  - `ExecuteSave()`: sau khi tính xong `C021`/`C022`/`C023`/`C024` (mọi nhánh —
    injection 2 lần cân, REX 1 lần cân, cascade multi-size), kiểm tra
    `(_rowSelected.C021 ?? 0) > 0` (dòng đã cân xong thật sự). Nếu đúng: tìm
    bước kế tiếp chưa cân (`AllowScale == true && C021 == 0`, sort theo
    `C015` rồi `C004` — cùng thứ tự dùng ở chỗ khác trong file) và gán vào
    `_rowSelected`; nếu không còn bước nào thì gán `_rowSelected = new
    FT600()` (field không nullable, các chỗ dùng đều qua `?.`/kiểm tra
    `AllowScale` nên an toàn — bấm Save khi rỗng sẽ bị chặn bởi check
    `!_rowSelected.AllowScale` đã có sẵn).
  - Thêm field `_lastWeighedRow` (nullable) — giữ tham chiếu tới dòng **vừa
    cân xong gần nhất**, gán ngay trước khi `_rowSelected` bị chuyển/xóa.
  - `ExecuteConfirmAsync()`: khối phát hiện multi-size mold REX (`pfx2` /
    `sameMolds` / `nonInjectionCheck` / `sizes`) trước đây dùng thẳng
    `_rowSelected` — với giả định ngầm là `_rowSelected` vẫn còn trỏ tới dòng
    cân cuối cùng lúc bấm Confirm. Sau khi thêm auto-advance ở trên, giả định
    đó không còn đúng (đặc biệt khi bước vừa cân là bước cuối → `_rowSelected`
    bị xóa về rỗng). Đổi toàn bộ khối này sang dùng `confirmRow =
    _lastWeighedRow ?? _rowSelected` để giữ nguyên hành vi cũ.
  - `ResetNewLoop()`: thêm `_lastWeighedRow = null;` cho gọn vòng lặp mới.

## Quyết định / giả định

- "Bước tiếp theo" được xác định là bước **chưa cân** gần nhất theo thứ tự
  `C015` (rồi `C004`) trong toàn bộ `_scaleDataFinal`, dùng đúng tiêu chí
  `C021 == 0` mà code hiện tại đã dùng khắp nơi để đánh dấu "chưa cân xong"
  (ví dụ đoạn cảnh báo "previous step has not been weighed"). Không giới hạn
  "bước tiếp theo" chỉ trong cùng 1 step code — nếu có nhiều size của cùng 1
  khuôn (multi-size) chưa cân, chúng cũng được coi là "bước tiếp theo" hợp lệ
  theo đúng tiêu chí này.
- Phát hiện thêm 1 phụ thuộc tiềm ẩn: `ExecuteConfirmAsync()` dùng
  `_rowSelected` để suy ra "bước cuối vừa cân" nhằm tìm REX multi-size mold
  cần nhân bản theo size — phụ thuộc này vốn đã mong manh từ trước (chỉ đúng
  nếu user không click chọn dòng khác trong grid trước khi bấm Confirm); thêm
  `_lastWeighedRow` giải quyết luôn cả trường hợp cũ đó, không chỉ vá riêng
  cho auto-advance mới.
- Không đổi ngữ nghĩa `_rowSelected` ở các luồng khác (chọn step qua QR/scan
  barcode, `OnGridRowSelected`, `OnGridReset`, `OnGridDelete`) — các luồng đó
  vẫn tự gán `_rowSelected` theo ý người dùng chọn, không bị auto-advance can
  thiệp.

## Việc còn mở

- Chưa build được để verify (xem mục Build/test) — cần build trên máy có
  quyền truy cập private NuGet feed chứa `ScanAndScale.Driver` rồi test lại
  luồng cân thực tế (đặc biệt case REX multi-size mold ở bước cuối) trước khi
  release.

## Build/test

`dotnet build SSSW/SSSW.sln -c Debug` fail ở bước restore:
`NU1101: Unable to find package ScanAndScale.Driver` — do máy dev hiện tại
không có quyền truy cập private feed chứa package này (môi trường sandbox),
không liên quan tới thay đổi code. Đã review lại toàn bộ diff bằng tay (đối
chiếu kiểu dữ liệu `C021`/`C028`/`AllowScale`, và mọi chỗ còn dùng
`_rowSelected` trong `ExecuteConfirmAsync`) thay vì build được.
