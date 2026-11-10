# Ảnh chụp màn hình kiểm thử Part Weight

Thư mục này chứa ảnh chụp màn hình khi chạy thử app theo từng ca bên dưới. **Hiện chưa có ảnh nào**: ca kiểm thử cần chạy app thật (DB, thẻ RFID, cân), môi trường dev không điều khiển được các thiết bị này. Khi có ảnh, đặt tên theo mẫu `<mã ca>_<bước>_<mô tả ngắn>.png` (ví dụ `T2_step4_base_ff.png`) và đánh dấu kết quả vào bảng.

Tài liệu liên quan: [Brainstorm](../../brainstorm/PART_WEIGHT_CHAIN_AND_LEVEL2.md) · [Plan](../../plan/PART_WEIGHT_CHAIN_AND_LEVEL2_PLAN.md) · ảnh người dùng gửi: [reference/](../reference/)

## Ca kiểm thử

Quy ước: N = Actual Partitioning, W = Total Weight (lần cân 1), S = số cân lần 2, g/chiếc Studs = 2, REX = 2.

| Mã | Ca | Cách làm | Kỳ vọng (g/prs) | Ảnh | Kết quả |
|---|---|---|---|---|---|
| T0 | Trước khi sửa (đối chứng) | Chạy bản `main` hoặc commit trước, cân FG `6112042313-A478-2801` step 1 → 5 | Ghi lại giá trị cũ để so sánh | | ☐ |
| T1 | BOM bình thường, cân tuần tự | FG `6112042313-A478-2801`, cân step 1 → 5, N = 1 | Logo 6 = 6; Studs 20 = 20; Inlay 45 − 8×2 = 29; Base_FF 90 − 45 − (2×2 + 8×2) = 25; Base_HF 110 − 90 − (Logo 2×g/chiếc + REX 4×2) | | ☐ |
| T2 | Step phân bổ ở nhiều step (Studs, Logo) | Cân Studs rồi Inlay, Base_FF, Base_HF | Studs dùng ở step 3, 4, 5 → trừ theo Level 2, không trừ step trước; Logo dùng ở step 5 → step 2 không trừ Logo | | ☐ |
| T3 | Step trước là chain nhưng chưa cân | Cân step 4 khi step 3 chưa cân | Báo cần cân step trước, không ghi | | ☐ |
| T4 | Chi tiết con Level 2 chưa cân | Cân step 3 khi Studs chưa cân | Báo thiếu Studs, không ghi | | ☐ |
| T5 | Cân lại step trước | Cân lại step 3 sau khi đã cân step 4 | Step 4 tự tính lại | | ☐ |
| T6 | 1 khuôn nhiều size | Khuôn có 2 size cùng step (vd. 10.5-11 và 8.5-9) | Công thức cũ (trừ các bước trước); runner chia lại giữa các size | | ☐ |
| T7 | 1 khuôn nhiều đôi cùng size | N = 2, W = 100, S = 80 | Total Part 80; Part Weight 80/2 − D = 40 − D; Runner (100 − 80)/2 = 10 | | ☐ |
| T8 | Vải nonwoven / mesh | Step có REX nonwoven | Công thức cũ, trừ REX cùng step | | ☐ |
| T9 | FG không có Level 2 | FG mà `sp_getBomWinlineOfItemFG` theo step trả rỗng | Step đầu bằng chính nó; step sau trừ step trước (chain) | | ☐ |
| T10 | REX chỉ có ở Level 2 | FG ví dụ | Dòng REX00018 ở đầu lưới (seq 0, Quantity 1, Pieces 12), cân được | | ☐ |
| T11 | Form nhập RFID tay | Nhập mã nhân viên rồi Enter | Tìm thấy thì form tự đóng | | ☐ |
| T12 | Lưu FT600 | Confirm sau khi cân | FG code/name = FG của step đang chọn; article/Main Item Name không trống | | ☐ |

## Cách chụp

- Chụp cả bảng TOTAL STEPS (kèm Row Details của step đang cân), phần SCALE VALUE và Reference Values.
- Với ca báo lỗi, chụp cả hộp thoại cảnh báo.
- Ghi số cân đã nhập (W, S, N) vào tên file hoặc ghi chú để đối chiếu với cột Kỳ vọng.
