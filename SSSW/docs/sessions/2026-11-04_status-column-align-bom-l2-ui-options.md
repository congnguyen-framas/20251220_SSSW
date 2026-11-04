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

## Việc còn mở
- Người dùng chọn phương án A; đã vẽ bản thiết kế lại `docs/image/bom_level2_option_A_v2_highlight.png` (highlight + mờ dòng khác, click lại để bỏ chọn, bỏ tiêu đề). Đã bỏ dòng self trong bảng chi tiết theo phản hồi (giữ hàng tiêu đề cột). **Chờ duyệt** rồi mới code.
- Chưa chạy app để xác nhận căn lề bằng mắt.
