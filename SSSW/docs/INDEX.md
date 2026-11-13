# 📚 SSSW Documentation Hub

> Tài liệu chuẩn hóa cho dự án **SSSW (Scan and Scale Shot Weight)** – phục vụ Vibe Coding với AI.

## Mục lục tài liệu

| Thư mục | Nội dung | Khi nào đọc |
|---------|----------|-------------|
| [`architecture/`](architecture/) | Kiến trúc hệ thống, sơ đồ thành phần, data flow | Trước khi thay đổi cấu trúc lớn |
| [`sessions/`](sessions/) | Nhật ký phiên làm việc (worklog) | Đầu mỗi phiên vibe code mới |
| [`brainstorm/`](brainstorm/) | Ý tưởng, đề xuất, thảo luận thiết kế | Khi brainstorm feature mới |
| [`plan/`](plan/) | Kế hoạch phát triển, roadmap, sprint plan | Khi lên kế hoạch task mới |
| [`tech/`](tech/) | Tài liệu kỹ thuật chuyên sâu (DB, công thức, API) | Khi cần hiểu logic nghiệp vụ |
| [`manual/`](manual/) | Manual Operator + Developer (EN/VI) kèm ảnh chú thích | Khi viết docs cho người dùng / dev mới |
| [`worklog/`](worklog/) | (Legacy) Worklog cũ – đã migrate sang sessions/ | Tham khảo lịch sử |

## Manual (User Manual)

| File | Đối tượng | Ngôn ngữ |
|------|-----------|----------|
| [`manual/OPERATOR_MANUAL.vi.md`](manual/OPERATOR_MANUAL.vi.md) | Operator (công nhân cân) | Tiếng Việt |
| [`manual/OPERATOR_MANUAL.en.md`](manual/OPERATOR_MANUAL.en.md) | Operator | English |
| [`manual/OPERATOR_MANUAL.id.md`](manual/OPERATOR_MANUAL.id.md) | Operator | Bahasa Indonesia |
| [`manual/DEVELOPER_MANUAL.vi.md`](manual/DEVELOPER_MANUAL.vi.md) | Developer | Tiếng Việt |
| [`manual/DEVELOPER_MANUAL.en.md`](manual/DEVELOPER_MANUAL.en.md) | Developer | English |

Ảnh trong `manual/images/` chụp từ màn hình thật với **dữ liệu giả lập**, đã đánh số chú thích; mã nguồn harness chụp ảnh ở `manual/screenshot-harness/` (đuôi `.cs.txt` – xem DEVELOPER_MANUAL §12).

## Quy trình Vibe Code

```
1. Đọc docs/sessions/ (entry mới nhất)
2. Đọc CLAUDE.md (context dự án)
3. Đọc docs/architecture/ nếu thay đổi cấu trúc
4. Code → Test → Commit
5. Ghi sessions/ entry mới
```

## File cốt lõi phải đọc trước khi code

1. **[`../../CLAUDE.md`](../../CLAUDE.md)** — Context chính cho AI, kiến trúc tổng quan
2. **[`VIBE_CODING_GUIDELINES.md`](VIBE_CODING_GUIDELINES.md)** — Bộ quy chuẩn và quy tắc Vibe Coding
3. **[`architecture/ARCHITECTURE.md`](architecture/ARCHITECTURE.md)** — Sơ đồ kiến trúc chi tiết
4. **[`tech/DATABASE_SCHEMA.md`](tech/DATABASE_SCHEMA.md)** — Schema DB và ý nghĩa cột
5. **[`tech/BUSINESS_LOGIC.md`](tech/BUSINESS_LOGIC.md)** — Công thức tính toán, luồng nghiệp vụ
6. **[`sessions/INDEX.md`](sessions/INDEX.md)** — Danh sách phiên làm việc (mới nhất lên trên)

## Quy ước chung

- Tất cả docs viết bằng Markdown
- Tên file: `kebab-case.md` hoặc `YYYY-MM-DD_slug.md` (cho sessions)
- Tiếng Việt hoặc Tiếng Anh đều được, nhất quán trong 1 file
- Commit docs cùng code changes liên quan
