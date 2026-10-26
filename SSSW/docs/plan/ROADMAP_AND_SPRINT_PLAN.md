# 🗺️ Roadmap & Sprint Plan

> Kế hoạch phát triển, cải tiến kỹ thuật và các mốc bàn giao cho hệ thống SSSW.

---

## 1. Mục tiêu Tổng thể (Master Goals)

1. **Độ ổn định dây chuyền (Floor Stability)**: Ứng dụng phải hoạt động liên tục 24/7 trên các máy trạm sản xuất tại các nhà máy fVN, fFT mà không bị crash hay xung đột kết nối cân.
2. **Đồng bộ dữ liệu thời gian thực (MES Synchronization)**: Dữ liệu shotweight sau khi cân được lưu an toàn vào SQL Server và cập nhật chính xác sang hệ thống ERP/MES Winline.
3. **Trải nghiệm Vibe Coding mượt mà (AI-Friendly Codebase)**: Codebase được module hóa rõ ràng, tài liệu đầy đủ, cho phép AI hỗ trợ code nhanh chóng và chính xác.

---

## 2. Lộ trình Triển khai (Milestones)

### Giai đoạn 1: Chuẩn hóa Codebase & Tài liệu (Sprint Hiện Tại - Hoàn tất)
- [x] Khảo sát toàn diện nhánh `feature/updateFT609`.
- [x] Tái cấu trúc thư mục tài liệu sang mô hình chuẩn: `architecture/`, `sessions/`, `tech/`, `brainstorm/`, `plan/`, `manual/`.
- [x] Tài liệu hóa Database Schema (`FT600` - `FT609`, SPs).
- [x] Tài liệu hóa công thức tính toán và business logic (`C021`, `C022`, `C023`, `C024`, `REX`, `Cleat_Ring`, `Inlay`).
- [x] Ban hành quy chế Vibe Coding tiêu chuẩn cao (`docs/VIBE_CODING_GUIDELINES.md`).

### Giai đoạn 2: Tách biệt Domain Logic & Xây dựng Bộ Unit Test (Đề xuất tiếp theo)
- [ ] Tách `ShotWeightCalculator` từ `ShotWeightViewModel` thành service độc lập.
- [ ] Tạo dự án test: `SSSW.Tests` (.NET 8 xUnit/NUnit).
- [ ] Viết test cases bao phủ:
  - Cân 2 lần chuẩn (Standard Injection).
  - Cân 1 lần với linh kiện REX.
  - Phép tính Cascade trừ khối lượng đinh với linh kiện `Cleat_Ring`.
  - Phân bổ trọng lượng khuôn nhiều size (Multi-size).
  - Kiểm tra vượt ngưỡng sai số (Tolerance check).

### Giai đoạn 3: Giả lập Phần cứng (Hardware Simulator) & Trải nghiệm Người dùng
- [ ] Bổ sung `MockDeviceService` / Chế độ Simulator trong môi trường Development.
- [ ] Hiện đại hóa thông báo: Thay thế toàn bộ `System.Windows.Forms.MessageBox` bằng WPF Custom Dialogs với giao diện tối ưu hóa cho màn hình cảm ứng công nghiệp.
- [ ] Bổ sung cơ chế tự phục hồi kết nối (Auto-reconnect) khi cân điện tử bị ngắt cáp LAN hoặc tắt nguồn đột ngột.

### Giai đoạn 4: Hoàn thiện tính năng FG Weighing & Tối ưu hóa Triển khai
- [ ] Hoàn thiện 100% quy trình cân thành phẩm (Finished Goods) trên tab `ShotWeightFGWindow`.
- [x] Soạn thảo đề xuất và chuẩn bị workflow CI/CD đóng gói bản cập nhật Release tự động qua GitHub Actions: xem [`CICD_GITHUB_ACTIONS_PROPOSAL.md`](CICD_GITHUB_ACTIONS_PROPOSAL.md) và workflow [`.github/workflows/build-and-release.yml`](../../.github/workflows/build-and-release.yml).
- [ ] Tham vấn bộ phận IT nhà máy về mô hình Hybrid (Self-hosted Runner) và kích hoạt pipeline.
