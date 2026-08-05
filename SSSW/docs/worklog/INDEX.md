# Worklog Index

Nhật ký các phiên làm việc trên codebase SSSW. Mỗi phiên (hoặc mỗi task đáng kể)
có **một file riêng** trong thư mục này (`docs/worklog/`), được liệt kê tại đây
theo thứ tự **mới nhất lên trên**.

Mục đích: khi bắt đầu một phiên làm việc mới (kể cả với Claude Code), đọc file
mới nhất (hoặc vài file gần nhất liên quan đến task đang làm) để nắm nhanh
trạng thái/quyết định gần đây, **không cần đọc lại toàn bộ code hoặc git log**.

Xem hướng dẫn quy ước tại cuối file này trước khi thêm entry mới.

## Danh sách phiên làm việc

| Ngày       | File                                                                     | Tóm tắt                                                                                  |
| ---------- | ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| 2026-08-06 | [2026-08-06_scanfg-window-and-deployconfig.md](2026-08-06_scanfg-window-and-deployconfig.md) | Thêm nút + cửa sổ WPF "Scan FG" (đổi tên → `ShotWeightFGWindow`); tách `DeployDir` ra `DeployConfig.props`. |

---

## Quy ước khi thêm entry mới

1. **Tên file**: `YYYY-MM-DD_slug-ngan-gon.md` (slug tiếng Anh, không dấu, nối bằng `-`).
   Nếu cùng ngày có nhiều task không liên quan, tách thành nhiều file (không dồn chung).
2. **Nội dung mỗi file** nên có các mục:
   - `## Yêu cầu` — người dùng yêu cầu gì (nguyên văn hoặc diễn giải ngắn).
   - `## Đã làm` — thay đổi cụ thể, kèm đường dẫn file (`SSSW/...`).
   - `## Quyết định / giả định` — chỗ nào tự suy đoán ý người dùng, đặt tên, chọn giải pháp khi có nhiều lựa chọn.
   - `## Việc còn mở` — câu hỏi chưa chốt, việc cố ý để lại cho phiên sau ("code sau", v.v.).
   - `## Build/test` — kết quả build cuối cùng (pass/fail, lỗi nào là thật vs. nhiễu môi trường).
3. Sau khi thêm file mới, **cập nhật bảng ở trên** (dòng mới lên đầu bảng, ngay dưới header).
4. File này (`INDEX.md`) và các file trong `worklog/` được **commit vào git bình thường** — không gitignore.
5. Không sửa lại nội dung các entry cũ trừ khi thông tin trong đó sai — worklog là nhật ký, không phải tài liệu sống (living doc); tài liệu sống là `ShotWeightWindow_Documentation_EN.md` và `CLAUDE.md`.
