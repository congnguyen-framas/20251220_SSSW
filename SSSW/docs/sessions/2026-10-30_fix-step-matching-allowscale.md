# 📝 Session: Fix Step Matching & AllowScale for BOM Sequential Steps

- **Ngày thực hiện**: 2026-10-30
- **Tác giả**: AI Assistant & ndcong
- **Mục tiêu**: Khắc phục lỗi bước cân cuối (Step 3: Rim, machine 0126) bị vô hiệu hóa nút Scale (`AllowScale = false`) sau khi quét tem QR code của cụm chi tiết hoặc khi chuyển bước.

---

## 1. Yêu cầu & Bối cảnh

- **Hiện tượng**:
  Khi quét mã QR `SW|20261030|0121|3-FB042602-PS08-2802|B1F10592-CD9F-4736-92EE-FF3B48782EE5|B0586F3A-6C87-4449-AA22-B4E115453003` vào màn hình chính cân linh kiện (`ShotWeightWindow.xaml`), danh sách gồm 3 bước:
  - Step 1: Studs (Machine 0113) - Status: Done
  - Step 2: Base (Machine 0121) - Status: Active / Done sau khi cân
  - Step 3: Rim (`4-FB042602-PMM9-2802`, Machine 0126) - Status: Pending
  Tuy nhiên, nút `[Scale]`, `[Reset]`, `[Delete]` tại dòng Step 3 luôn bị mờ (disabled), công nhân không thể bấm nút Scale để chọn cân tiếp bước 3.
- **Ràng buộc**:
  - Tuân thủ nghiêm ngặt **Zero Logic Mutation** trong `AGENTS.md` (giữ nguyên 100% công thức `C021`, `C022`, `C023`, `C024`).

---

## 2. Phân tích & Nguyên nhân gốc rễ

1. **Chuỗi so khớp không có `.Trim()`**:
   Dữ liệu trả về từ SQL Server (`FT601.C004`, `FT601.C020`) và BOM Winline (`BomWinlineModel.ItemStepCode`, `BomWinlineModel.ItemFgCode`) thường có khoảng trắng đệm hoặc độ dài cố định kiểu `CHAR`. Phép so sánh trực tiếp `x.C004 == item.ItemStepCode && x.C020 == item.ItemFgCode.Split('-')[0]` bị trả về `false`.
2. **Khóa cứng `allowScale = false` trong nhánh fallback**:
   Khi `ckHydra == null`, code tìm kiếm theo fallback `x.C007.Contains($"{mc}-") || x.C004.Contains($"-{smc}-")`. Dù tìm thấy bản ghi tương ứng trong `_dataHydra` (Machine `0126`), code vẫn thực thi dòng gán cứng `allowScale = false;`.
   Dẫn đến khi đưa vào `_scaleDataFinal`, dòng Step 3 nhận `AllowScale = false`, làm cho UI WPF vô hiệu hóa hoàn toàn nút `Scale` qua binding `IsEnabled="{Binding AllowScale}"`.
3. **Dropdown chọn bước bị chặn bởi `_qrCodeScan`**:
   Trong `OnStepSelectedAsync`, điều kiện `if (string.IsNullOrEmpty(_qrCodeScan))` ngăn cản cập nhật `_stepSelected` khi người dùng chủ động chọn bước khác từ LookUpEdit `cbStepName`.

---

## 3. Các thay đổi đã thực hiện

File chỉnh sửa: [`SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs`](file:///d:/0.Works/1.SourceCode/0.Projects/20251220_SSSW/SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs)

1. **Chuẩn hóa chuỗi và so khớp thông minh (`GetDataAsync`)**:
   - Sử dụng `.Trim()` và `StringComparison.OrdinalIgnoreCase` khi so sánh mã bước `ItemStepCode` và mã chính `C020` / `C007`.
   - Tìm kiếm linh hoạt hơn:
     ```csharp
     var stepCodeTrim = item.ItemStepCode?.Trim() ?? string.Empty;
     var fgMainCodeTrim = item.ItemFgCode?.Split('-')[0]?.Trim() ?? string.Empty;

     ckHydra = stepCodeTrim.Equals(_stepSelected.C004?.Trim(), StringComparison.OrdinalIgnoreCase)
         ? _stepSelected
         : _dataHydra.FirstOrDefault(x =>
             x.C004?.Trim().Equals(stepCodeTrim, StringComparison.OrdinalIgnoreCase) == true &&
             (x.C020?.Trim().Equals(fgMainCodeTrim, StringComparison.OrdinalIgnoreCase) == true ||
              (!string.IsNullOrEmpty(fgMainCodeTrim) && x.C007?.Contains(fgMainCodeTrim) == true)));
     ```
2. **Bỏ gán cứng `allowScale = false`**:
   - Trong nhánh fallback, chỉ khi thực sự không tìm thấy lệnh sản xuất trong `_dataHydra` mới đặt `allowScale = false`:
     ```csharp
     allowScale = ckHydra != null;
     ```
3. **Hỗ trợ chọn bước thủ công từ LookUpEdit (`OnStepSelectedAsync`)**:
   - Cho phép cập nhật `_stepSelected` khi người dùng chọn bước khác trên combobox dù trước đó đã quét mã QR, đồng thời tự động reset barcode cũ nếu đổi bước:
     ```csharp
     if (string.IsNullOrEmpty(_qrCodeScan) || selected.StepItemCode?.Trim() != _stepSelected?.C004?.Trim())
     {
         _stepSelected = _dataHydra.FirstOrDefault(x =>
             x.C004?.Trim() == selected.StepItemCode?.Trim() &&
             x.C015?.Trim() == selected.Machine?.Trim() &&
             x.C018 == selected.HydraOrderNo)
             ?? _dataHydra.FirstOrDefault(x => x.C004?.Trim() == selected.StepItemCode?.Trim())
             ?? throw new Exception("Step not found in master data.");

         if (!string.IsNullOrEmpty(_qrCodeScan) && _stepSelected.Id != _labelInfo?.C000)
         {
             _qrCodeScan = string.Empty;
             _labelInfo = new FT606_Label();
             ClearBarcodeAction?.Invoke();
         }
     }
     ```
4. **Chuẩn hóa so khớp khi chuyển bước sau khi đã load bảng cân**:
   - Dùng `.Trim()` khi tìm `rowSelect` trong `_scaleDataFinal` để tránh lỗi "Label does not match the item being weighed".

---

## 4. Kết quả kiểm thử (Build Verification)

- **Lệnh build**: `dotnet build SSSW/SSSW.csproj -c Debug --no-restore`
- **Kết quả**: `Build succeeded. 0 Error(s)`.
- Không làm thay đổi bất kỳ công thức tính toán `C021`, `C022`, `C023`, `C024` hay phá vỡ kiến trúc DB.

---

## 5. Lưu ý vận hành (Operational Notes)

- **Cân vi sai (Cascade Subtraction)**:
  Ở Step 2 (Base), cụm chi tiết lấy từ máy `0121` đã chứa sẵn đinh Studs (Step 1 = 18.71g). Vì vậy tổng khối lượng part thực tế khi cân trên đĩa phải $> 18.71\text{g}$ thì khối lượng nhựa Base (`C021 = TotalPart - PrevStuds`) mới ra giá trị dương hợp lệ.

