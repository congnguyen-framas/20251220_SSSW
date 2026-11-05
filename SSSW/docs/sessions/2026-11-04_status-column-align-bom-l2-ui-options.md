# 📝 Session: Căn cột STATUS + mockup hiển thị BOM Level 2

- **Ngày**: 2026-11-04 · **Nhánh**: `feature/use-bom-level-2`

## Yêu cầu
1. Căn đều cột STATUS với các cột khác trong TOTAL STEPS (vị trí mũi tên trong ảnh người dùng gửi).
2. Gợi ý cách hiển thị BOM level 2, chụp ảnh lưu `docs/image` để review.

## Đã làm
- [`ShotWeightWindow.xaml`](../../UI/WPF/ShotWeightWindow.xaml): `StackPanel` ô STATUS đổi `Margin="0,0,0,14"` → `Margin="0,10"`. Trước đó chữ Pending/Active bị đẩy lên ~6px so với MACHINE/Step No.
- 3 mockup + [`brainstorm/BOM_LEVEL2_UI_OPTIONS.md`](../brainstorm/BOM_LEVEL2_UI_OPTIONS.md); ảnh ở `docs/image/bom_level2_option_*.png` (dựng bằng HTML, chụp Edge headless — không phải ảnh chụp app thật).

## Quyết định / giả định
- Hiểu "gạch chân đều nhau" là căn thẳng hàng theo chiều dọc của ô STATUS với các cột còn lại; nếu ý khác (đường kẻ ngang giữa các dòng) cần ảnh rõ hơn.

## Đã làm (tiếp) — UI BOM Level 2 (phương án A v3)
- Mockup v8 (`docs/image/bom_level2_option_A_v8_actions_wide.png`, thay v3–v7; viền chọn đủ 4 cạnh, lưới chi tiết chỉ có đường kẻ ngang giữa các dòng (giống ảnh mẫu), ăn sát 8 cột, dòng được chọn hiện Active; cột Actions nới 125 → 140px để 3 nút 1 hàng, `WrapPanel` tự rớt dòng khi kéo hẹp; trước đó: vẽ đủ cột của lưới thật; giữ 8 cột như v3, cột 1–5 căn theo Actions / STATUS / MACHINE / MAIN / Step No. (Qty dời sang trái 1 cột, Unit dịch theo), cột 6–8 (Weighed, g / piece, Deduct) chia đều bề rộng cột STEP / STEP CODE). Code hiện có vẫn theo v3 — chờ duyệt v7 mới sửa: cột chi tiết sắp lại theo thứ tự xanh và căn cùng đường dọc với cột lưới chính (bảng map ở `brainstorm/BOM_LEVEL2_UI_OPTIONS.md`).
- Code: `models/Level2DetailItem.cs` (mới); `FT600.partial.cs` (+`Level2Items`, `Level2Formula`, `Level2HasMissing`); `ShotWeightViewModel.RefreshLevel2Details()` gọi trong `RefreshStepsGridCore`; `ShotWeightStyles.xaml` (`StepRowStyle`, `Level2DetailsTemplate`, converter `NullToCollapsed`); `ScaleConverters.cs` (`NullToCollapsedConverter`); `ShotWeightWindow.xaml` (+ thuộc tính `dgTotalSteps`); `ShotWeightWindow.xaml.cs` (làm mờ dòng, click lại để bỏ chọn).
- Hành vi: chọn step → viền nổi + xổ chi tiết, dòng khác mờ; click lại → bỏ chọn; sau Save/Reset lưới được dựng lại và step đang cân tự được chọn lại (xổ chi tiết cập nhật trạng thái cân).

- Tài liệu thiết kế riêng: [`plan/BOM_LEVEL2_ROW_DETAILS_UI.md`](../plan/BOM_LEVEL2_ROW_DETAILS_UI.md) (ảnh thiết kế, bảng map cột, khả thi, mermaid, checklist review).

## Đã làm (tiếp) — dọn giao diện màn cân (code)
- [`ShotWeightWindow.xaml`](../../UI/WPF/ShotWeightWindow.xaml) — `dgTotalSteps`: `GridLinesVisibility="None"` để bỏ các đường gạch chân lệch nhau dưới từng ô (do `DataGridCell` có `VerticalAlignment=Center` nên đường kẻ của ô nằm sát dưới nội dung, không nằm ở đáy dòng).
- [`ShotWeightStyles.xaml`](../../UI/WPF/Styles/ShotWeightStyles.xaml) — `DataGridCell`: `FocusVisualStyle=null` (bỏ khung nét đứt ở ô được focus).
- REFERENCE VALUES: bỏ khối ghi chú chữ (`DeltaInformation`) dưới bảng; chuyển vào **tooltip của icon "i"** ở góc phải tiêu đề card (hover là hiện, không delay, `MaxWidth` 460 tự xuống dòng).
- Bố cục cột phải: `RowDefinition` TOTAL STEPS `428` → `*` (`MinHeight=320`), hai card REFERENCE / HISTORY và REFERENCE VALUES về chiều cao cố định `520` → card TOTAL STEPS rộng thêm ~70px ở màn hình ~1080px.
- Row Details: cột **Component tự xuống dòng theo từ** khi dài (bỏ cắt "…"), chiều cao dòng chi tiết `MinHeight=30` tự giãn; dòng công thức cũng tự xuống dòng.

## Review logic khóa nút cân (`AllowScale`)
- Người dùng yêu cầu rà lại logic khóa nút Scale/Reset/Delete (ảnh: Logo và Studs bị mờ). Kết quả phân tích ghi ở [`tech/ALLOWSCALE_LOGIC_REVIEW.md`](../tech/ALLOWSCALE_LOGIC_REVIEW.md): cách tính, 4 chỗ kiểm tra "bước trước phải cân" không thống nhất, 7 nhận xét (N1–N7), 5 đề xuất (A–E), 5 câu hỏi. **Chưa sửa code, chờ review.**

## Bỏ khóa nút cân + insert lại có đổi FG + icon mô tả card (code)
- Quyết định (chi tiết ở [`tech/ALLOWSCALE_LOGIC_REVIEW.md`](../tech/ALLOWSCALE_LOGIC_REVIEW.md) §0): A = chỉ chặn theo chi tiết con Level 2; Studs cân tại chỗ; insert lại dòng cũ nhưng đổi FG code/name; bỏ khóa hoàn toàn.
- [`ShotWeightViewModel.cs`](../../UI/WPF/ViewModels/ShotWeightViewModel.cs): xóa vùng khóa/lọc theo loại step (và `allowScale = ckHydra != null`); thêm `RequiresPreviousStepsWeighed()` / `PreviousStepsToCheck()` thay 4 chỗ kiểm tra "mọi bước trước"; đánh dấu `IsReusedFromHistory` khi nạp dòng cũ; Confirm đổi `C013`/`C014` cho các dòng đó.
- [`FT600.partial.cs`](../../models/ef/entities/FT600.partial.cs): thêm `IsReusedFromHistory` (`[NotMapped]`).
- UI: style `InfoIcon` (ShotWeightStyles.xaml) + icon "i" với tooltip mô tả ở STEP INFORMATION, TOTAL STEPS, REFERENCE / HISTORY; REFERENCE VALUES dùng chung style.
- Docs: BUSINESS_LOGIC §4.4 và §6, DATABASE_SCHEMA, ShotWeightWindow_Documentation_EN §4.6.3.
- Build `SSSW.csproj` 0 error (lần build đầu thường lỗi file tạm WPF `wpftmp`, build lại thì sạch); chưa chạy app thật.

## Sửa mờ dòng Active + đề xuất Quantity / Weight per Unit
- Sửa lỗi dòng Active bị mờ khi vào màn hình lần đầu: `RowOpacity` so khớp theo `row.Item` thay vì `IsSelected` (cờ chưa gán lúc `LoadingRow`) và áp dụng lại độ mờ sau layout (`ShotWeightWindow.xaml.cs`).
- Tìm nguyên nhân Inlay `2-VB042313-A123-2801` có giá trị cân dù FT600 không có: truy vấn nạp "lần cân trước" khớp theo mã model + cùng size + cùng `C015` nên mượn bản ghi khác (và Confirm sẽ ghi lại bản ghi đó dưới mã step này). Ghi ở [`plan/QTY_WEIGHT_PER_PIECE_LEVEL2.md`](../plan/QTY_WEIGHT_PER_PIECE_LEVEL2.md) §1 kèm SQL kiểm tra và 3 cách sửa.
- Đề xuất gộp `Quantity` theo BOM Level 2 và tính `Weight per Unit = C021 ÷ Quantity` (cùng file, §2). **Chưa code, chờ duyệt.**

## Quantity gộp theo BOM Level 2 (`C039`) + khớp trọng lượng cũ theo đúng mã step (code)
- Quyết định (chi tiết ở [`plan/QTY_WEIGHT_PER_PIECE_LEVEL2.md`](../plan/QTY_WEIGHT_PER_PIECE_LEVEL2.md) §4): khớp đúng `C002`; cột mới `C039` (số chiếc / 1 đôi), `C025` giữ nguyên; step cuối cũng tính; cân đủ số lượng 1 đôi.
- [`FT600.cs`](../../models/ef/entities/FT600.cs): thêm `C039` (`double?`). **Người dùng tự tạo cột trong DB** (`ALTER TABLE FT600/FT600_dev ADD C039 float NULL`).
- Mới `UI/WPF/Services/ComponentQuantityAggregator.cs` (gộp `Σ qty(p→c)`, `PiecesFor`); `PartWeightCalculator.UnitWeightPerPiece` dùng `C036`, dự phòng `C021 ÷ C039` (bỏ giả định PRS = 2 chiếc).
- `ShotWeightViewModel`: `ApplyPieceQuantities()` (gán `C039`, tính `C036`, chuẩn hóa bản ghi cũ), `UpdateWeightPerPiece()`, `PiecesOf()`; cập nhật `C036` khi Save / Cascade; vòng nạp "lần cân trước" chỉ khớp đúng `C002`.
- Test: `tests/SSSW.Tests` viết lại theo nghĩa mới + `ComponentQuantityAggregatorTests` (chưa chạy được vì NuGet nội bộ không truy cập).
- Docs: BUSINESS_LOGIC §4.8, DATABASE_SCHEMA, ShotWeightWindow_Documentation_EN. Build `SSSW.csproj` 0 error; chưa chạy app thật (cần tạo cột `C039` trước khi chạy).

## Cột `C039` trên lưới TOTAL STEPS
- `ShotWeightWindow.xaml` — `dgTotalSteps`: thêm cột `Pieces Qty (PCS)` (`C039`, rộng 80, có tooltip ở tiêu đề) vào đúng vị trí cột `Quantity` cũ; cột `Quantity` (`C025`) dời xuống cuối lưới. Docs: `ShotWeightWindow_Documentation_EN.md` §6.3.1, plan `QTY_WEIGHT_PER_PIECE_LEVEL2.md` §4. Build 0 error; **chưa commit/push** (theo yêu cầu mới của người dùng).

## Reference/History tự chuyển + REX chỉ có ở Level 2 + đổi chữ công thức (code, chưa commit)
- Sau khi cân xong, hệ thống tự chuyển sang step kế tiếp nhưng Reference / History không đổi theo (chỉ cập nhật khi bấm nút Scale trong lưới). `ExecuteSave` giờ mở panel và gọi `LoadRefHistoryAsync` cho step mới (`AllowPartitionAdj = false`, `IsHistoryExpanded = true`, giống `OnGridScale`).
- REX chỉ thấy trong BOM Level 2 nay được đưa ra lưới để cân: `AddLevel2OnlyRexRows()` tạo **một dòng cho mỗi mã REX**, `C015` bằng step chứa nó (nhiều step cùng chứa thì lấy step có thứ tự nhỏ nhất), đặt ngay trước step đó; thông tin lệnh/máy/FG lấy từ step chứa nó, mã/tên/category/đơn vị/số lượng lấy từ Level 2. Số chiếc cân = `C039` gộp (vd. REX00018 = 8 + 4 = 12). Nếu mã REX đã có trong FT600 thì điền sẵn trọng lượng như cách nạp REX Level 1 (`PrefillRexFromHistory`). Giả định mình đặt: một dòng cho mỗi mã REX (không tách theo từng step) để khỏi cân trùng.
- Công thức hiển thị dưới bảng chi tiết đổi chữ: `Part Weight = Total Part Weight − (…)` (trước là `C021 = C023 − (…)`).

## Đề xuất quản lý version qua DB
- Người dùng yêu cầu version `x.y.z` (bắt đầu 1.2.0), quản lý theo từng máy trong bảng DB, release ghi vào DB để các máy tự cập nhật, revert được, migration tự tạo bảng. Vì chạm vào luồng tự cập nhật và pipeline build Release nên chỉ viết đề xuất: [`plan/APP_VERSIONING_DB_UPDATE.md`](../plan/APP_VERSIONING_DB_UPDATE.md) (bảng `FT610_AppRelease` / `FT611_AppMachine`, migration bằng `AppVersionDbContext` riêng, luồng cập nhật/revert, rủi ro, 7 câu hỏi). **Chưa code, chờ duyệt.**

## Việc còn mở
- Người dùng chọn phương án A; đã vẽ bản thiết kế lại `docs/image/bom_level2_option_A_v2_highlight.png` (highlight + mờ dòng khác, click lại để bỏ chọn, bỏ tiêu đề). Đã bỏ dòng self trong bảng chi tiết theo phản hồi (giữ hàng tiêu đề cột). Mockup v2 đã chuyển toàn bộ cột và phần mô tả sang tiếng Anh. **Chờ duyệt** rồi mới code.
- Chưa chạy app để xác nhận bằng mắt: căn lề cột STATUS và Row Details (cột chi tiết thẳng hàng, mờ dòng, click lại bỏ chọn). Build `SSSW.csproj` 0 error (build ra thư mục tạm vì `SSSW.exe` đang chạy).
- Cột Component chỉ rộng 125px (cột Actions): tên dài bị cắt, tooltip hiện mã chi tiết; chip Status 65px.

## 2026-11-05: form RFID tay, article/FG khi lưu FT600, viền card
- **Form nhập RFID tay** (`frmRfidInput`): tra cứu thấy nhân viên (hoặc đăng ký tên mới xong) thì tự đóng, không cần bấm OK (`frmRfidInputViewModel.OnRfidValueChanged/OnRfidNameEnterAsync` trả `bool`).
- **Article / Main Item Name trống**: step không có dòng Hydra riêng (chỉ khớp "mờ" theo model/size) trước đây mượn article của lệnh khác (cùng một article `2-VB042313-2801` cho mọi step). Nay: khớp không đúng mã step -> `C005` = mã step bỏ đoạn màu (`4-VB042313-A123-2801` -> `4-VB042313-2801`); `C027` trống -> tên step bỏ loại step và màu. Step chọn từ master FT601 hoặc khớp đúng mã giữ nguyên. REX trống lấy theo step cùng thứ tự.
- **FG code sai khi lưu**: nguyên nhân là `ckHydra` khớp mờ (`C007` chứa mã main, hoặc `C004` chứa `-smc-`) lấy FG của lệnh khác (B915) cho dòng của step đang cân (A478). Nay khi Confirm, mọi dòng insert (trừ dòng cùng khuôn khác size, `_sameMoldRowIds`) lưu FG code/name của step đang scan/chọn; trước đây chỉ dòng nạp lại từ lịch sử.
- **Viền đáy card**: 2 card dưới (Reference/History, Reference Values) đặt `Margin` đáy 0 nên thấp hơn card Step Information 4px -> đổi thành 4.
- Chưa chạy app thật; build 0 error.

### Bổ sung 2026-11-05 (lần 2)
- **Form RFID không tự đóng**: `OperatorSessionService.OnRfidValueChanged` không set lại `RfidCardCode` khi thành công nên VM gán chuỗi rỗng (ô ID bị xóa, điều kiện đóng sai). Nay giữ mã vừa gõ -> form tự đóng.
- **REX chỉ có ở Level 2**: dòng đưa ra lưới có `C015 = 0` (REX có thể thuộc nhiều step), đặt đầu danh sách, `C025 = 1`, số chiếc gộp theo Level 2 nằm ở `C039`.

### Bổ sung 2026-11-05 (lần 3): Part Weight cho 1 khuôn nhiều size / nonwoven
- **Đã rút lại phần "nhiều size"** (xem lần 5 bên dưới): chỉ còn nonwoven/mesh dùng công thức cũ.
- Luồng cân và chặn cân theo BOM Level 2 giữ nguyên. Chỉ riêng cách **tính Part Weight (C021)** của step thuộc nhóm 1 khuôn nhiều size (`IsMultiSizeMold`) hoặc có vải nonwoven/mesh (`HasNonwovenRex`) dùng lại công thức cũ (`UsesLegacyPartWeight` → `ApplyLegacyPartWeight`: C023 − Σ C021 các bước trước − Σ C021 non-injection cùng step; REX nonwoven chỉ có ở Level 2 (dòng C015 = 0) vẫn được trừ). Cascade Level 2 bỏ qua các step này để không ghi đè. Các step này cũng phải cân xong mọi step trước (`RequiresPreviousStepsWeighed`) vì công thức cũ cần Part Weight các bước trước.
- Confirm: thêm kiểm tra null khi không có dòng REX cùng step cho nhóm nhiều size.

### Bổ sung 2026-11-05 (lần 4): 1 khuôn nhiều đôi cùng size (Actual Partitioning N)
- Số cân lần 2 (S) là tổng part của cả N đôi. Công thức mới: **`C023 = S` (không chia N)**, `C021 = C023 / N − Σ level 2` (= S/N − D, D tính cho 1 đôi; helper `PartPerPair`), `C022 = (C024 − S) / N` (Runner = YES). N = Actual Partitioning (C028, theo giá trị đã chỉnh), N rỗng/0 coi là 1. Trước đây part weight chưa chia N nên runner ra âm (vd. W=100, S=80, N=2: cũ −30, mới 10). Với nhiều size, S của từng size là tổng N đôi của size đó; phần chia lại runner giữa các size nay là `(C024 − Σ C023) / Σ C028` (C023 là tổng của N đôi).

### Bổ sung 2026-11-05 (lần 5): nhiều size không còn trừ bước trước
- Step 8.5-9 (cùng step 2, cùng máy/khuôn với step 10.5-11) bị trừ Part Weight bước trước dù Level 2 "No components": do `UsesLegacyPartWeight` gồm cả `IsMultiSizeMold` (thêm ở lần 3). Đã bỏ: nhiều size tính theo Level 2 như mọi step; chỉ nonwoven/mesh còn dùng công thức cũ. `IsMultiSizeMold` vẫn dùng cho phân bổ runner giữa các size khi Save.
- Quy tắc chốt (đã khôi phục 1 khuôn nhiều size theo yêu cầu: không đụng vào): 1 khuôn nhiều size và nonwoven/mesh -> công thức cũ (kèm cascade cũ và điều kiện cân xong các bước trước); còn lại theo BOM Level 2; không có Level 2 (hoặc không có chi tiết con) -> Part Weight = chính nó (`C023/N`), không trừ bước trước, không chặn theo bước trước. Đã bỏ cascade cũ (trừ bước trước khi không nạp được Level 2) và nhánh công thức cũ khi không có Level 2.
