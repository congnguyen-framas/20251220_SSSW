# 📝 Session: 2026-10-26 - Chuẩn hóa Documentation cho Vibe Coding

## 1. Yêu cầu & Bối cảnh
- **Người dùng yêu cầu**:
  > "review code hiện tại tại nhánh 'feature/updateFT609' rồi chuẩn hóa lại các thông tin để vibe code, gồm Docs chứa toàn bộ secssion, worklog, brainstorm, plan, tech, manual. Và các quy định để vibe code chuẩn"
- **Nhánh hiện tại**: `feature/updateFT609`
- **Mục tiêu**:
  - Khảo sát sâu toàn bộ codebase SSSW (.NET 8 WPF/WinForms).
  - Tái cấu trúc thư mục tài liệu `docs/` thành các module chuyên biệt: `architecture/`, `sessions/`, `tech/`, `brainstorm/`, `plan/`, `manual/`.
  - Cung cấp tài liệu kỹ thuật chi tiết về CSDL, công thức tính toán và nghiệp vụ cân.
  - Ban hành bộ quy định Vibe Coding chuẩn để AI và lập trình viên phối hợp mượt mà, hạn chế tối đa lỗi hồi quy (regression) và nhầm lẫn ngữ cảnh.

---

## 2. Phân tích Hiện trạng Codebase (`feature/updateFT609`)

### Điểm mạnh:
1. **Kiến trúc Shell hiện đại**: Đã quy hoạch thành công cửa sổ vỏ `Main.xaml` (WPF) điều phối 2 màn hình chính: `ShotWeightWindow` (Step Component) và `ShotWeightFGWindow` (Finished Goods).
2. **Quản lý thiết bị tập trung (Singleton Pattern)**: `DeviceConnectionService` và `OperatorSessionService` xử lý đúng vòng đời phần cứng (Barcode, Scale, RFID), tránh xung đột cổng COM và socket TCP/IP.
3. **Multi-site Deployment**: Tách riêng cấu hình share deployment ra `DeployConfig.props`, cho phép chuyển đổi giữa các nhà máy (`fVN`, `fFT`...) mà không cần sửa file `.csproj`.

### Điểm cần cải thiện / Nợ kỹ thuật:
1. **Tài liệu phân tán**: Worklog cũ nằm rải rác trong `docs/worklog/`, thiếu tài liệu mô tả bảng CSDL (`FT600` - `FT609`), thiếu giải thích chi tiết các cột bí danh (`C021`, `C022`, `C023`, `C024`).
2. **ViewModel còn nặng logic (Fat ViewModel)**: `ShotWeightViewModel.cs` có hơn 2.100 dòng code, trộn lẫn giữa truy vấn database, validation, UI message boxes và các phép tính đặc biệt (`REX`, `Cleat_Ring`, `Inlay`).
3. **Chưa có Unit Test**: Toàn bộ dự án phụ thuộc vào việc test thủ công trên máy thực tế hoặc máy ảo kết nối thiết bị.

---

## 3. Các thay đổi đã thực hiện

1. **Khởi tạo và quy hoạch Hub tài liệu `SSSW/docs/`**:
   - `docs/INDEX.md`: Cổng trung tâm điều hướng toàn bộ hệ sinh thái tài liệu.
   - `docs/architecture/ARCHITECTURE.md`: Mô hình kiến trúc chi tiết, vòng đời khởi động và luồng dữ liệu.
   - `docs/tech/DATABASE_SCHEMA.md`: Từ điển dữ liệu toàn bộ các bảng `FT...` và Stored Procedures.
   - `docs/tech/BUSINESS_LOGIC.md`: Quy chuẩn nghiệp vụ cân, công thức tính `C021` - `C024`, các trường hợp ngoại lệ (`Cleat_Ring`, `REX`, `Inlay`, Multi-size).
   - `docs/sessions/INDEX.md` & Session này: Chuẩn hóa quy trình ghi nhận nhật ký phát triển.
   - `docs/brainstorm/BRAINSTORM_ARCH_AND_FEATURES.md`: Đề xuất cải tiến và lộ trình tái cấu trúc Clean Architecture.
   - `docs/plan/ROADMAP_AND_SPRINT_PLAN.md`: Kế hoạch nâng cấp và kiểm thử.
   - `docs/plan/CICD_GITHUB_ACTIONS_PROPOSAL.md`: Đề xuất kỹ thuật chi tiết trình IT nhà máy về chuyển đổi CI/CD & Auto-Update qua GitHub Actions (Mô hình Hybrid an toàn cho mạng xưởng OT).
   - `.github/workflows/build-and-release.yml`: File cấu hình GitHub Actions đóng gói tự động 2 bản Release (`fVN` và `fFT`).
   - `docs/manual/OPERATOR_MANUAL.md`: Cẩm nang hướng dẫn thao tác cho công nhân vận hành.
   - `docs/VIBE_CODING_GUIDELINES.md`: Bộ quy chuẩn vàng dành cho Vibe Coding với AI, đặc biệt nhấn mạnh nguyên tắc **Bảo toàn tuyệt đối Business Logic hiện tại (Zero Logic Mutation)** — mọi tối ưu hóa/refactor bắt buộc giữ nguyên 100% kết quả như logic hiện tại, tuyệt đối không được làm thay đổi.

2. **Cập nhật `CLAUDE.md` gốc**:
   - Tích hợp hướng dẫn điều hướng vào Hub tài liệu mới.
   - Bổ sung quy tắc bắt buộc đọc `docs/` trước khi bắt đầu code bất kỳ tính năng nào.

---

## 4. Quyết định kỹ thuật / Giả định
- Giữ nguyên thư mục cũ `docs/worklog/` để làm tư liệu lịch sử (legacy), không xóa bỏ mà tạo `docs/sessions/` làm chuẩn tiếp nối chính thức từ thời điểm này.
- Giữ nguyên các quy ước đặt tên cột nguyên bản của database Framas/Hydra (`C000`, `C001`...) để bảo đảm tương thích 100% với các stored procedures hiện hữu của hệ thống MES.

---

## 5. Kết quả kiểm thử (Build / Check)
- Chạy kiểm tra git status: Nhánh `feature/updateFT609` hoạt động ổn định, tài liệu được tích hợp đồng bộ và sẵn sàng commit.

---

## 6. Việc còn mở / Kế hoạch tiếp theo
- Triển khai unit test cho bộ công thức tính toán `ShotWeightCalculator` tách riêng khỏi ViewModel.
- Xem xét migrate các dialog MessageBox từ WinForms sang WPF Native Dialog để giao diện đồng nhất.
