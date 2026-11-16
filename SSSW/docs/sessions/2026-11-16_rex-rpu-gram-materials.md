# 📝 Session: REX / RPU tiêu hao theo gram của step PU

- **Ngày**: 2026-11-16 · **Nhánh**: `feature/use-bom-level-2`

## 1. Yêu cầu
Đinh sắt/keo (mã `REX`, `RPU`, đơn vị G) không hiện ở Level 2 và bị thiếu RPU ở Total Steps. Phương án A: REX/RPU nhóm Raw Materials (category 34, 101 — cấu hình trên OMS trong `CategoryOfNonInjectionUsagePartial`) được cân/trừ như REX non-woven; **logic cân các step hiện tại không được đổi**. Có lịch sử → nạp; không có → giá trị BOM Level 2, `%` = 100; cân mới/đổi % → theo giá trị mới; category chưa cấu hình → báo admin.

## 2. Thay đổi
- `PartWeightCalculator`: `IsNonInjectionCode`, `IsGramMaterial`; `IsSelfRow` không coi REX/RPU đơn vị G là self; `Calculate` trừ phần sử dụng đã cân (`C021`) hoặc `Quantity` BOM nếu chưa cân.
- `ShotWeightViewModel`: `IsNonInj`/`IsGramRaw` thay `StartsWith("REX")` trong luồng Save/Confirm/trạng thái; `AddLevel2OnlyRexRows` nhận RPU và cảnh báo category chưa cấu hình; `HasNonwovenRex` và nhánh multi-size bỏ qua dòng G; Row Details hiện dòng G (đơn vị G, mặc định BOM); `%` mặc định 100, đổi `%` chỉ áp dòng đang chọn; `C036` dòng G = phần sử dụng; nạp lịch sử dùng `C024`.
- `FT600.IsPu` loại cả RPU. Test: 5 test mới (22/22 pass).
- Docs: `tech/BUSINESS_LOGIC.md` §4.8.1; `manual/REX_RPU_TEST_REVIEW.md` + 9 ảnh `manual/images/17*.png`; harness cảnh `raw`.

## 3. Phát hiện khi chụp test
Mã không có `-` cho `PrefixUpToSecondHyphen` rỗng → REX/RPU cùng máy/khuôn/step 0 bị coi là "cùng khuôn nhiều size" và ghi đè nhau (runner âm, total sai). Đã loại dòng G khỏi nhánh đó.

## 3b. Ba lỗi từ test thật cùng ngày (đã sửa, xem doc review §6)
- Step No. của dòng REX/RPU hiện `0` → `FT600.StepNoText` hiện `REX`/`RPU` (XAML bind `StepNoText`, sort theo `C015`).
- 1 khuôn nhiều size + non-woven dùng chung: size thứ 2 không bị trừ → `ApplyLegacyPartWeight` gộp BOM Level 2 của mọi size cùng khuôn.
- Cân lại REX/RPU sau khi PU đã cân: Runner sai do `CascadeLevel2` gán `C022 = C024 − C021` → bỏ, chỉ tính lại `C021`.
- Chưa chạy được test mới bằng `dotnet test` vì app SSSW.exe đang chạy khóa `bin\Debug`; đã build ra thư mục riêng và kiểm chứng bằng harness (9 ảnh 17* + 4 ảnh 18*).

## 3c. Hoàn thiện hiển thị REX/RPU (test thật lần 2)
- Row Details: cột Step ghi `RPU` cho dòng RPU. Lưới: Pieces Qty = 1, Weight per Unit mặc định = khối lượng BOM Level 2 (chưa lịch sử). Confirm không chặn khi REX/RPU (g) chưa cân.
- Ảnh: `19a-raw-pcs-weight-per-unit.png`, `17b-raw-bom-default.png` (chụp lại).

## 4. Việc còn mở
- Review `manual/REX_RPU_TEST_REVIEW.md`; test với DB/OMS thật (admin thêm category 34, 101).
- Nhãn đơn vị `g` ở cột Pieces Qty của lưới chưa đổi.
- Chưa commit (chờ yêu cầu).
