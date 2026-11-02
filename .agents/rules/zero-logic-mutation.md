# ⚠️ Rule: Zero Logic Mutation (Bảo toàn tuyệt đối Business Logic)

## Phạm vi áp dụng
Áp dụng cho mọi tác vụ code, refactor, tối ưu hóa hoặc chỉnh sửa trong solution SSSW (đặc biệt là `ShotWeightViewModel.cs`, `ShotWeightFGViewModel.cs`, và các model tính toán).

## Quy định bắt buộc
1. **Không làm thay đổi kết quả tính toán**:
   Mọi tối ưu hóa hoặc tái cấu trúc thuật toán tính trọng lượng (`C021`, `C022`, `C023`, `C024`) phải đảm bảo trả về kết quả số học giống 100% logic hiện tại trong mọi trường hợp (Test Injection, REX, Cleat_Ring, Inlay, Multi-size).
2. **Không đổi tên cột CSDL**:
   Các cột mang tên quy ước `C000`, `C001`, `C021`, `C024`... trên các Entity EF Core (`FT600` đến `FT609`) phản ánh trực tiếp schema vật lý của SQL Server MES framas. Tuyệt đối không tự ý đổi tên cột.
3. **Tuân thủ quy trình kiểm tra tuần tự**:
   Bắt buộc giữ nguyên logic kiểm tra các bước cân trước (`C015 < current.C015`) phải có trọng lượng (`C021 > 0`) trước khi cho phép cân và lưu bước sau.
4. **Giữ nguyên cascade filter**:
   Filter linh kiện `Cleat_Ring` phải kiểm tra chính xác `StartsWith("Cleat_Ring")`.
