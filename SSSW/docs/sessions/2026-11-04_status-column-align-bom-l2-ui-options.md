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
- Mockup v4 (`docs/image/bom_level2_option_A_v4_real_columns.png`, thay v3 vì v3 lược bớt cột của lưới thật): cột chi tiết sắp lại theo thứ tự xanh và căn cùng đường dọc với cột lưới chính (bảng map ở `brainstorm/BOM_LEVEL2_UI_OPTIONS.md`).
- Code: `models/Level2DetailItem.cs` (mới); `FT600.partial.cs` (+`Level2Items`, `Level2Formula`, `Level2HasMissing`); `ShotWeightViewModel.RefreshLevel2Details()` gọi trong `RefreshStepsGridCore`; `ShotWeightStyles.xaml` (`StepRowStyle`, `Level2DetailsTemplate`, converter `NullToCollapsed`); `ScaleConverters.cs` (`NullToCollapsedConverter`); `ShotWeightWindow.xaml` (+ thuộc tính `dgTotalSteps`); `ShotWeightWindow.xaml.cs` (làm mờ dòng, click lại để bỏ chọn).
- Hành vi: chọn step → viền nổi + xổ chi tiết, dòng khác mờ; click lại → bỏ chọn; sau Save/Reset lưới được dựng lại và step đang cân tự được chọn lại (xổ chi tiết cập nhật trạng thái cân).

- Tài liệu thiết kế riêng: [`plan/BOM_LEVEL2_ROW_DETAILS_UI.md`](../plan/BOM_LEVEL2_ROW_DETAILS_UI.md) (ảnh thiết kế, bảng map cột, khả thi, mermaid, checklist review).

## Việc còn mở
- Người dùng chọn phương án A; đã vẽ bản thiết kế lại `docs/image/bom_level2_option_A_v2_highlight.png` (highlight + mờ dòng khác, click lại để bỏ chọn, bỏ tiêu đề). Đã bỏ dòng self trong bảng chi tiết theo phản hồi (giữ hàng tiêu đề cột). Mockup v2 đã chuyển toàn bộ cột và phần mô tả sang tiếng Anh. **Chờ duyệt** rồi mới code.
- Chưa chạy app để xác nhận bằng mắt: căn lề cột STATUS và Row Details (cột chi tiết thẳng hàng, mờ dòng, click lại bỏ chọn). Build `SSSW.csproj` 0 error (build ra thư mục tạm vì `SSSW.exe` đang chạy).
- Cột Component chỉ rộng 125px (cột Actions): tên dài bị cắt, tooltip hiện mã chi tiết; chip Status 65px.
