# ⚡ Vibe Coding Guidelines for SSSW Project

> Bộ quy tắc chuẩn hóa dành cho Lập trình viên và AI Assistant (Claude, Gemini, ChatGPT...) khi thực hiện **Vibe Coding** trên dự án SSSW.
> Mục tiêu: Tốc độ phát triển thần tốc nhưng vẫn đảm bảo **100% tính an toàn**, **không phát sinh bug hồi quy (regression)** và **giữ vững tính nhất quán của kiến trúc**.

---

## 1. Nguyên Tắc Cốt Lõi (Core Principles)

1. **Hiểu Ngữ Cảnh Trước Khi Chạm Vào Code**:
   - Luôn đọc `SSSW/docs/INDEX.md` và file session gần nhất trong `SSSW/docs/sessions/` trước khi bắt đầu.
   - Không tự ý phán đoán kiến trúc hoặc tên cột database khi chưa đọc `SSSW/docs/tech/DATABASE_SCHEMA.md` và `SSSW/docs/tech/BUSINESS_LOGIC.md`.
2. **Không Phá Vỡ Quy Ước Database Của Hệ Thống Cũ**:
   - Các bảng `FT600` đến `FT609` sử dụng tên cột theo định dạng Framas MES (`C000`, `C001`, `C021`, `C024`...). Tuyệt đối **không tự ý đổi tên cột** hoặc cố tình refactor tên cột trong entity class trừ khi có yêu cầu rõ ràng.
3. **Bảo Tồn Toàn Vẹn Chú Thích & Worklog**:
   - Giữ nguyên các comment giải thích nghiệp vụ, đặc biệt là các chú thích đánh dấu lý do sửa lỗi lịch sử (VD: cascaded filter, Inlay calculation, REX logic...).
4. **Bảo Toàn Tuyệt Đối Business Logic Hiện Tại (Zero Logic Mutation)**:
   - Toàn bộ các công thức tính toán (`C021`, `C022`, `C023`, `C024`), luồng cân, điều kiện kiểm tra tuần tự (sequential check), logic xử lý ngoại lệ (`REX`, `Cleat_Ring`, `Inlay`, Multi-size mold...) đã được căn chỉnh chính xác theo nghiệp vụ nhà máy và kiểm chứng trong thực tế sản xuất.
   - **Mọi hành động tối ưu hóa, tái cấu trúc (refactor) hoặc dọn dẹp mã nguồn**: bắt buộc phải bảo đảm **giữ nguyên 100% kết quả đầu ra như logic hiện tại, tuyệt đối không được phép thay đổi**.
   - Tuyệt đối không tự ý thay đổi, đơn giản hóa, hoặc viết lại cách tính toán nếu không có yêu cầu rõ ràng từ người dùng.

---

## 2. Quy Trình Vibe Coding 5 Bước (The 5-Step Workflow)

```
┌────────────────────────────────────────────────────────┐
│ 1. Đọc Session mới nhất & Context liên quan            │
│    (docs/sessions/INDEX.md, CLAUDE.md)                 │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│ 2. Lập kế hoạch thay đổi (Impact Analysis)             │
│    • Chỉ ra các file sẽ chạm vào                       │
│    • Đánh giá tác động đến luồng phần cứng/UI          │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│ 3. Thực thi thay đổi (Coding)                          │
│    • Tuân thủ MVVM và Dependency Injection             │
│    • Giữ hàm ngắn gọn, tập trung xử lý đơn nhiệm       │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│ 4. Kiểm thử & Thẩm định (Verify Build)                 │
│    • Build Debug/Release kiểm tra lỗi cú pháp/DI       │
│    • Đối chiếu lại kịch bản edge cases                 │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│ 5. Ghi nhận Session mới (Session Logging & Commit)     │
│    • Tạo file docs/sessions/YYYY-MM-DD_slug.md         │
│    • Cập nhật bảng docs/sessions/INDEX.md              │
│    • Commit git với message rõ ràng, chuẩn semantic    │
└────────────────────────────────────────────────────────┘
```

---

## 3. Quy Ước Lập Trình (Coding Standards)

### 3.1. WPF & MVVM
- **Không code-behind**: Mọi tương tác người dùng, binding dữ liệu và commands phải thông qua ViewModel (`ShotWeightViewModel`, `ShotWeightFGViewModel`, `MainViewModel`).
- **Phân biệt kiểu dữ liệu WinForms và WPF**:
  - Vì dự án bật cả `<UseWindowsForms>true</UseWindowsForms>` và `<UseWPF>true</UseWPF>`, các namespace như `Brush`, `Color`, `Point`, `MessageBox` sẽ bị xung đột.
  - Phải luôn khai báo alias rõ ràng ở đầu file:
    ```csharp
    using Brush = System.Windows.Media.Brush;
    using Brushes = System.Windows.Media.Brushes;
    using Color = System.Windows.Media.Color;
    ```
- **Xử lý luồng UI (Dispatcher)**: Khi nhận sự kiện từ background thread (sự kiện cân TCP/IP hoặc quét mã vạch), phải luôn cập nhật UI thông qua `Application.Current.Dispatcher.InvokeAsync(...)`.

### 3.2. Quản lý Phần cứng (Hardware Drivers)
- **Tuyệt đối không khởi tạo mới** `BarcodeDriver`, `RfidDriver` hoặc `ScaleDriver` bên trong các UserControl con.
- Tất cả phải sử dụng thông qua singleton `DeviceConnectionService` được tiêm từ DI hoặc truy cập qua `GlobalVariable.Devices`.

### 3.3. Multi-site Deployment & Environment
- Khi cần chuyển đổi môi trường từ **fVN** sang **fFT** hoặc ngược lại:
  1. Chỉnh sửa đường dẫn trong `SSSW/DeployConfig.props`.
  2. Bật/tắt chuỗi kết nối tương ứng trong `SSSW/appsettings.json`.
  3. Tuyệt đối không chỉnh sửa đè thẻ `<DeployDir>` trực tiếp trong file `SSSW.csproj`.

---

## 4. Quy Ước Commit Git (Commit Conventions)

Mỗi commit phải có thông điệp rõ ràng theo định dạng:
```
<type>(<scope>): <mô tả ngắn gọn bằng tiếng Anh hoặc tiếng Việt>

[Tùy chọn: mô tả chi tiết các thay đổi]
```

- `feat`: Tính năng mới (VD: `feat(fg): Add finished goods weighing tolerance check`)
- `fix`: Sửa lỗi (VD: `fix(calc): Fix Cleat_Ring cascade subtraction typo`)
- `refactor`: Tái cấu trúc code không làm thay đổi tính năng (VD: `refactor(shell): Centralize RFID operator session`)
- `docs`: Cập nhật tài liệu (VD: `docs(hub): Standardize documentation structure for vibe coding`)
- `chore`: Cập nhật cấu hình build, deploy (VD: `chore(deploy): Switch active site to fFT in DeployConfig.props`)

---

## 5. Quy Định Tạo & Cập Nhật Session

- Bất kỳ phiên làm việc nào có thay đổi code logic **đều bắt buộc** phải tạo file session trong `SSSW/docs/sessions/` và cập nhật `SSSW/docs/sessions/INDEX.md`.
- File session đóng vai trò là "bộ nhớ ngoài" (External Memory) của dự án, giúp AI ở phiên làm việc sau chỉ cần đọc 1 file là nắm bắt ngay toàn bộ bối cảnh mà không cần quét lại toàn bộ repository.
