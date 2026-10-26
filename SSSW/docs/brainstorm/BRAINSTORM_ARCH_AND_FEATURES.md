# 💡 Brainstorm: Architectural Improvements & Feature Ideas

> Nơi lưu trữ các ý tưởng, đề xuất kỹ thuật và thảo luận thiết kế trước khi đưa vào kế hoạch thực thi chính thức (`docs/plan/`).

---

## 1. Tách tách biệt Logic tính toán ra khỏi ViewModel (Domain Calculator Service)

### Vấn đề hiện tại:
- `ShotWeightViewModel.cs` hiện dài hơn 2.100 dòng code.
- Toàn bộ công thức tính toán `C021`, `C022`, `C023`, `C024`, các trường hợp `REX`, `Cleat_Ring`, `Inlay`, và kiểm tra sai số (Tolerance) đều bị gắn chặt với UI state và `System.Windows.Forms.MessageBox`.
- **Hệ quả**: Không thể viết Unit Test kiểm thử logic tính toán nếu không khởi chạy toàn bộ WPF/WinForms context.

### Đề xuất giải pháp:
- Tạo một Domain Service thuần túy (POCO C#):
  ```csharp
  public interface IShotWeightCalculator
  {
      ShotWeightCalculationResult CalculateInjectionShot(double grossWeight, double runnerWeight, int pairsPerShot, double percentUsage);
      ShotWeightCalculationResult CalculateRexShot(double grossWeight, int pairsPerShot);
      void ApplyCleatRingCascade(List<FT600> steps, FT600 currentSelected);
      ToleranceCheckResult CheckTolerance(double actualPartWeight, double standardWeight, ToleranceConfig config);
  }
  ```
- Lợi ích:
  - Có thể viết hàng chục Unit Test bao phủ mọi kịch bản góc (edge cases) của khuôn mẫu.
  - ViewModel giảm bớt ~500 dòng code phức tạp, chỉ làm nhiệm vụ binding dữ liệu và điều hướng UI.

---

## 2. Tham khảo mẫu Clean Architecture từ thư mục `Template/`

Trong thư mục `Template/ShotweightEnterprise_1000Users_CleanArchitecture`, dự án đã có sẵn mẫu thiết kế Clean Architecture phân tầng:
```
ShotweightEnterprise1000Users/
├── Application/     # Use Cases, Interfaces (IStepRepository...)
├── Domain/          # Core Business Entities, Logic
├── Infrastructure/  # EF Core DbContext, Hardware Repositories
└── Presentation/    # WPF Views & ViewModels
```

### Hướng phát triển dài hạn:
- Từng bước chuẩn hóa cấu trúc của `SSSW/` theo mô hình tách tầng này khi bổ sung các tính năng lớn tiếp theo.
- Loại bỏ hoàn toàn sự phụ thuộc vào WinForms (`System.Windows.Forms.MessageBox` và các form cũ như `frmMainView`). Thay thế bằng WPF Material Design / DevExpress Dialogs hiện đại.

---

## 3. Mock Hardware & Chế độ Mô phỏng (Hardware Simulator / Debug Mode)

### Vấn đề:
- Lập trình viên AI hoặc developer khi ngồi viết code tại văn phòng không có sẵn:
  - Đầu đọc Barcode Zebra thực tế.
  - Cân điện tử Shimadzu / Vibra kết nối qua cổng mạng LAN / COM.
  - Đầu đọc thẻ RFID công nhân.
- Điều này khiến việc kiểm thử luồng sự kiện (event-driven) gặp nhiều khó khăn.

### Đề xuất:
- Thêm một chế độ **`DebugSimulator`** trong `DeviceConnectionService`:
  - Khi bật flag `"EnableSimulator": true` trong `appsettings.Development.json`:
  - Mở một thanh công cụ nhỏ trên UI (floating panel) cho phép giả lập:
    - Bấm nút "Quét Barcode mẫu" (gửi chuỗi barcode giả lập).
    - Bấm nút "Quẹt thẻ RFID" (gửi mã thẻ giả lập).
    - Slider chỉnh trọng lượng cân (phát event `ScaleChanged` giả lập cân đang nhảy số).
- Điều này sẽ giúp việc **Vibe Coding với AI** tăng tốc độ gấp 5 - 10 lần vì có thể tái hiện và kiểm chứng mọi tình huống tức thì.
