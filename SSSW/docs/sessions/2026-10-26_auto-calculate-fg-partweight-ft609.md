# 📝 Phiên làm việc: Tự động lưu FT609 khi cân bước cuối thông qua store sp_ShotweightReport_UpsertFT609

- **Ngày thực hiện**: 2026-10-26
- **Nhánh Git**: `feature/updateFT609`
- **Người thực hiện**: Antigravity (Pair Programming with User)
- **File tài liệu liên quan**:
  - [`SSSW/docs/tech/DATABASE_SCHEMA.md`](../tech/DATABASE_SCHEMA.md)
  - [`SSSW/docs/tech/BUSINESS_LOGIC.md`](../tech/BUSINESS_LOGIC.md)
  - [`SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs`](../../UI/WPF/ViewModels/ShotWeightViewModel.cs)

---

## 1. Yêu cầu & Bối cảnh

- **Prompt gốc của User**:
  > Logic của tính năng này:
  > - Khi cân thì cần kiểm tra xem với con item FG đó bước đang cân có phải là bước cuối hay chưa, nếu chưa phải là bước cuối thì bỏ qua.
  > - Khi bước đang chọn cân là bước cuối rồi thì gọi store:
  >   `EXEC sp_ShotweightReport_UpsertFT609 @FGItemCode = '...', @CreatedBy = '...';`
  >   là nó sẽ tự chạy quy trình và xử lý trong store không cần làm gì cả, thay `CreatedBy` bằng `employName`.

- **Mục tiêu**:
  1. Kiểm tra chính xác bước đang cân/chọn cân (`_stepSelected`) có phải là bước cuối cùng của mã FG hay không (`C015 == Max(C015)`).
  2. Nếu **chưa phải là bước cuối**: bỏ qua (skip), không gọi store và không ảnh hưởng đến luồng cân bình thường.
  3. Nếu **là bước cuối**: sau khi dữ liệu cân `FT600` được lưu và commit thành công, tự động thực thi Stored Procedure:
     ```sql
     EXEC sp_ShotweightReport_UpsertFT609 @FGItemCode = @fgCode, @CreatedBy = @employName;
     ```
     với `@employName` lấy từ thông tin thẻ RFID quét của người vận hành (`_operatorSession.OperatorInfo.C001`).
  4. Tuân thủ nghiêm ngặt **Zero Logic Mutation**: Tuyệt đối không thay đổi các công thức cân của từng bước thành phần (`C021`, `C022`, `C023`, `C024`).

---

## 2. Phân tích & Đánh giá

### 2.1 Cơ chế phân định bước cuối
- Mỗi bước cân của một con FG có thứ tự song song `C015` (`ParallelSequence` từ BOM).
- Bước cuối cùng hoàn thiện sản phẩm FG có `C015` đạt giá trị lớn nhất trong nhóm bước của FG đó:
  $$\text{maxSequence} = \max_{\text{step} \in \text{FG}} (\text{C015})$$
- Bước đang chọn cân trong UI (`_stepSelected`) hoặc bước nằm trong danh sách xác nhận (`insert` với `C021 > 0`) được so khớp với `maxSequence`:
  - Nếu `C015 < maxSequence`: Là bước trung gian -> Bỏ qua.
  - Nếu `C015 == maxSequence`: Là bước cuối cùng -> Thực thi store.

### 2.2 Vị trí thực thi Stored Procedure
- Trong `ExecuteConfirmAsync`, `FT600` và `FT601` được lưu và commit trước (`await transaction.CommitAsync()`).
- Sau khi `FT600` đã được lưu chắc chắn vào SQL Server, phương thức `ProcessUpsertFT609ForFinalStepsAsync` được gọi.
- Phương thức này mở một kết nối và thực thi `sp_ShotweightReport_UpsertFT609`. Toàn bộ quá trình tính Part Weight, Runner Weight, Shot Weight và ghi vào bảng `FT609` được xử lý tập trung trong Store của SQL Server MES.
- Được bọc trong khối `try..catch` với logging chi tiết (`_logger.LogInformation` / `_logger.LogError`), đảm bảo nếu store gặp ngoại lệ thì máy trạm cân vẫn hoạt động trơn tru, không chặn người vận hành.

---

## 3. Các thay đổi đã thực hiện

### 3.1 `SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs`

1. **Tích hợp vào `ExecuteConfirmAsync`**:
   ```csharp
   await db.SaveChangesAsync();
   await transaction.CommitAsync();

   // ── Tự động gọi sp_ShotweightReport_UpsertFT609 nếu bước đang cân là bước cuối ──
   await ProcessUpsertFT609ForFinalStepsAsync(insert);

   System.Windows.Forms.MessageBox.Show("Scale shot weight saved successfully.", "Information",
       (MessageBoxButtons)MessageBoxButton.OK, (MessageBoxIcon)MessageBoxImage.Information);
   ```

2. **Cài đặt phương thức `ProcessUpsertFT609ForFinalStepsAsync`**:
   - Lấy tên người vận hành:
     ```csharp
     var employName = !string.IsNullOrWhiteSpace(_operatorSession?.OperatorInfo?.C001)
         ? _operatorSession.OperatorInfo.C001
         : (!string.IsNullOrWhiteSpace(_operatorSession?.OperatorInfo?.C000)
             ? _operatorSession.OperatorInfo.C000
             : Environment.UserName);
     ```
   - Nhóm dữ liệu cân theo FG Code và tìm bước cuối:
     ```csharp
     var maxSequence = fgGroup.Max(x => x.C015);
     var finalStep = fgGroup.FirstOrDefault(x => x.C015 == maxSequence);
     ```
   - Kiểm tra bước đang cân:
     ```csharp
     var isSelectedStepFinal = (_stepSelected != null && !string.IsNullOrEmpty(_stepSelected.C004) && _stepSelected.C004 == finalStep.C002)
         || (_stepSelected?.C010 != null && _stepSelected.C010 == maxSequence);

     var hasFinalStepInInsert = insert.Any(x =>
         (!string.IsNullOrEmpty(x.C013) ? x.C013 : _stepItemCodeScale.C007) == fgCode &&
         x.C015 == maxSequence &&
         (x.C021 ?? 0) > 0);

     if (!isSelectedStepFinal && !hasFinalStepInInsert)
     {
         _logger.LogInformation("FG {FGCode}: buoc dang can khong phai buoc cuoi. Bo qua.", fgCode);
         continue;
     }

     if (!hasFinalStepInInsert) continue;
     ```
   - Thực thi store:
     ```csharp
     await db.Database.ExecuteSqlRawAsync(
         "EXEC sp_ShotweightReport_UpsertFT609 @FGItemCode = {0}, @CreatedBy = {1}",
         fgCode, employName);
     ```

---

## 4. Quyết định kỹ thuật / Giả định

1. **Chuyển giao tính toán cho Store**: Store `sp_ShotweightReport_UpsertFT609` phụ trách 100% việc tổng hợp số liệu trong DB, giảm tải xử lý phía client và đảm bảo nhất quán logic giữa các hệ thống (WPF, Web, MES).
2. **Xác thực bước cuối an toàn kép**: Kiểm tra cả `_stepSelected` (bước chọn trên UI) và dòng dữ liệu trong `insert` (`C021 > 0` và `C015 == maxSequence`), tránh trường hợp gọi store khi bước cuối chưa thực sự được cân xong.

---

## 5. Kết quả kiểm thử (Build Verification)

- **Lệnh build**: `dotnet build SSSW/SSSW.sln`
- **Kết quả**:
  - `Build succeeded: 0 Error(s)`
  - Cảnh báo: `164 Warning(s)` (toàn bộ là warning nullable gốc của codebase, giảm 2 warning so với phiên trước).
  - Thời gian build: ~18 giây.

---

## 6. Việc còn mở / Bàn giao

1. Test thực tế trên máy cân với dữ liệu FG 1 bước và FG nhiều bước (multi-step):
   - Cân bước 1: Xác nhận không gọi store, không sinh bản ghi trong `FT609`.
   - Cân bước cuối: Xác nhận store được gọi thành công, `FT609` được tạo/cập nhật với `CreatedBy` là tên nhân viên trên thẻ RFID.
