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

## Việc còn mở
- Người dùng chọn phương án A; đã vẽ bản thiết kế lại `docs/image/bom_level2_option_A_v2_highlight.png` (highlight + mờ dòng khác, click lại để bỏ chọn, bỏ tiêu đề). Đã bỏ dòng self trong bảng chi tiết theo phản hồi (giữ hàng tiêu đề cột). Mockup v2 đã chuyển toàn bộ cột và phần mô tả sang tiếng Anh. **Chờ duyệt** rồi mới code.
- Chưa chạy app để xác nhận bằng mắt: căn lề cột STATUS và Row Details (cột chi tiết thẳng hàng, mờ dòng, click lại bỏ chọn). Build `SSSW.csproj` 0 error (build ra thư mục tạm vì `SSSW.exe` đang chạy).
- Cột Component chỉ rộng 125px (cột Actions): tên dài bị cắt, tooltip hiện mã chi tiết; chip Status 65px.
