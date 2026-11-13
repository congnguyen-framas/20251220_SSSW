# 📝 Sessions Index (Nhật Ký Phiên Làm Việc)

> Nhật ký theo dõi toàn bộ các phiên làm việc và phát triển trên codebase SSSW.
> Mỗi phiên làm việc mới (hoặc task lớn) có **1 file riêng** trong thư mục `docs/sessions/`, được cập nhật vào bảng dưới đây theo thứ tự **mới nhất lên trên**.

---

## Danh sách các phiên (Sessions)

| Ngày | File Session | Tóm tắt mục tiêu & kết quả |
|------|--------------|---------------------------|
| **2026-11-13** | [`2026-11-13_user-manuals-operator-developer.md`](2026-11-13_user-manuals-operator-developer.md) | User Manual Operator + Developer (EN/VI) có ảnh chú thích chụp từ màn hình thật với dữ liệu giả lập; harness chụp ảnh trong `manual/screenshot-harness/`. |
| **2026-11-12** | [`2026-11-12_bom-l2-match-by-main-code.md`](2026-11-12_bom-l2-match-by-main-code.md) | Ghép child BOM Level 2 với step theo mã chính (bỏ màu & size, REX giữ nguyên); nhiều size thì ưu tiên cùng size step cha. |
| **2026-11-10** | [`2026-11-10_pu-pcs-weighing.md`](2026-11-10_pu-pcs-weighing.md) | Cân hàng PU (`FT601.C016='PU'`) theo chiếc: Unit `PCS-L`/`PCS-R` theo hậu tố mã step, g/chiếc, nhãn UI pcs. |
| **2026-11-04** | [`2026-11-04_status-column-align-bom-l2-ui-options.md`](2026-11-04_status-column-align-bom-l2-ui-options.md) | Căn cột STATUS trong TOTAL STEPS; 3 mockup hiển thị BOM Level 2 (`docs/image`, `brainstorm/BOM_LEVEL2_UI_OPTIONS.md`) chờ chốt. |
| **2026-11-02** | [`2026-11-02_bom-level2-partweight.md`](2026-11-02_bom-level2-partweight.md) | Tính `C021` theo BOM Level 2 (`PartWeightCalculator`), tách service MVVM (Dialog/Bom/Repository), thêm test xUnit; entity tạm trỏ `FT600_dev`. |
| **2026-10-30** | [`2026-10-30_fix-step-matching-allowscale.md`](2026-10-30_fix-step-matching-allowscale.md) | Khắc phục lỗi bước cân cuối (Step 3: Rim, machine 0126) bị vô hiệu hóa nút Scale (`AllowScale = false`) sau khi quét tem QR code hoặc khi chuyển bước. |
| **2026-10-26** | [`2026-10-26_auto-calculate-fg-partweight-ft609.md`](2026-10-26_auto-calculate-fg-partweight-ft609.md) | Nhận diện bước cân cuối cùng của FG (`Max(C015)`), tự động gọi store `sp_ShotweightReport_UpsertFT609` với tên nhân viên cân và bỏ qua nếu chưa phải bước cuối. |
| **2026-10-26** | [`2026-10-26_standardize-documentation-for-vibe-coding.md`](2026-10-26_standardize-documentation-for-vibe-coding.md) | Khảo sát toàn diện nhánh `feature/updateFT609`, chuẩn hóa toàn bộ tài liệu dự án (Architecture, Sessions, Tech, Plan, Brainstorm, Manual) và ban hành quy chuẩn Vibe Coding tiêu chuẩn cao. |
| *Legacy* | [`../worklog/INDEX.md`](../worklog/INDEX.md) | Các phiên làm việc lịch sử (giai đoạn 08/2026: tối ưu hóa shell Main, tách tab Step/FG, fix cascade Cleat_Ring, cấu hình fFT). |

---

## Hướng dẫn tạo Session mới cho AI & Developer

1. **Đặt tên file**: `YYYY-MM-DD_slug-ngan-gon.md` (chữ thường, gạch nối `-`).
2. **Cấu trúc chuẩn một session file**:
   - `## 1. Yêu cầu & Bối cảnh`: Bài toán cần giải quyết, user prompt gốc.
   - `## 2. Phân tích & Đánh giá`: Code hiện tại, các điểm rủi ro.
   - `## 3. Các thay đổi đã thực hiện`: Liệt kê chi tiết file sửa/thêm, logic thay đổi.
   - `## 4. Quyết định kỹ thuật / Giả định`: Tại sao chọn cách này mà không chọn cách khác.
   - `## 5. Kết quả kiểm thử (Build / Test)`: Lệnh build, cảnh báo, kết quả chạy thực tế.
   - `## 6. Việc còn mở / Bàn giao`: Task cần làm tiếp trong phiên sau.
3. **Luôn cập nhật lại bảng trên** ngay sau khi hoàn tất phiên.
