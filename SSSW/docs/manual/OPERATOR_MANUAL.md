# 📖 Operator Manual (Hướng Dẫn Vận Hành)

> Cẩm nang hướng dẫn thao tác dành cho công nhân và kỹ thuật viên vận hành trạm cân Shot Weight trên dây chuyền sản xuất.

---

## 1. Khởi động Ứng dụng & Đăng nhập

1. Mở ứng dụng **SSSW** từ biểu tượng trên màn hình Desktop.
2. Kiểm tra thanh tiêu đề phía trên cùng:
   - Hiển thị tên nhà máy (VD: `fFT` hoặc `fVN`) và phiên bản phần mềm.
   - Các đèn trạng thái thiết bị ở góc phải:
     - **Barcode**: Màu xanh lá (Đã kết nối).
     - **Scale**: Màu xanh lá (Đã kết nối và nhận tín hiệu trọng lượng).
     - **RFID**: Màu xanh lá (Sẵn sàng nhận diện thẻ).
3. **Đăng nhập**:
   - Quẹt thẻ nhân viên qua đầu đọc RFID.
   - Tên và mã số nhân viên sẽ hiển thị tại ô thông tin Operator trên thanh header.
   - *Nếu đầu đọc thẻ có sự cố*: Bấm trực tiếp vào vùng tên/mã nhân viên để mở hộp thoại nhập thủ công mã nhân viên.

---

## 2. Quy trình Cân Linh Kiện (Step Component Tab)

### Bước 1: Quét mã vạch (Barcode / QR Code)
- Dùng máy quét mã vạch quét mã QR trên lệnh sản xuất hoặc tem dán linh kiện.
- Danh sách các linh kiện / bước cân tương ứng với khuôn sẽ tự động hiển thị trên bảng dữ liệu.

### Bước 2: Thực hiện Cân theo từng bước (Step)
Hệ thống sẽ bôi đậm bước cần cân theo thứ tự từ Step 1 đến các Step tiếp theo:

#### A. Đối với linh kiện ép nhựa thông thường (2 lần cân):
1. **Lần cân 1 (Total Shot Weight)**:
   - Đặt nguyên cả shot (gồm toàn bộ chi tiết nhựa và cuống nhựa / runner) lên mặt bàn cân.
   - Chờ số trên cân ổn định. Bấm nút **[SCALE]** (hoặc phím tắt).
   - Giá trị tổng sẽ được ghi nhận vào cột `GROSS WEIGHT (g)`.
2. **Lần cân 2 (Runner Weight)**:
   - Bỏ các chi tiết sản phẩm ra, **chỉ giữ lại cuống nhựa (Runner)** trên mặt cân.
   - Chờ số ổn định và bấm **[SCALE]**.
   - Trọng lượng cuống nhựa ghi vào cột `RUNNER (g)`.
   - Phần mềm sẽ tự động tính toán ra trọng lượng chi tiết `PART (g/prs)`.

#### B. Đối với linh kiện đinh chèn sẵn (Mã bắt đầu bằng `REX`):
- Chỉ cần đặt chi tiết lên cân và bấm **[SCALE] 1 lần duy nhất** (không có bước cân cuống nhựa).

#### C. Đối với linh kiện vòng đệm (`Cleat_Ring`):
- Thực hiện cân theo hướng dẫn. Hệ thống sẽ tự động trừ đi trọng lượng của đinh REX đã cân ở bước trước để ra khối lượng nhựa thực tế.

---

## 3. Kiểm tra Sai số & Lưu kết quả

1. **Đối chiếu sai số (Tolerance)**:
   - Cột so sánh sẽ hiển thị độ lệch so với trọng lượng chuẩn (BOM Standard).
   - Nếu màu xanh: Đạt chuẩn trong ngưỡng dung sai cho phép.
   - Nếu màu vàng / đỏ: Báo động sai số vượt ngưỡng, cần báo kỹ thuật khuôn kiểm tra lại áp suất / thông số máy ép.
2. **Lưu kết quả (Save)**:
   - Sau khi hoàn thành đủ các bước bắt buộc, nút **[SAVE]** sẽ sáng lên.
   - Bấm **[SAVE]**.
   - Hộp thoại xác nhận hiện ra: *"Scale shot weight saved successfully."*
   - Dữ liệu được ghi nhận vào hệ thống và form tự động sẵn sàng cho lượt quét tiếp theo.

---

## 4. Xử lý các Sự cố Thường gặp (Troubleshooting)

| Hiện tượng | Nguyên nhân | Cách xử lý |
|------------|-------------|------------|
| **Đèn Scale màu đỏ / Disconnected** | Mất kết nối mạng LAN hoặc cân bị tắt nguồn | Kiểm tra dây mạng cắm vào cân điện tử, bật lại nguồn cân, bấm nút cấu hình để kết nối lại. |
| **Không bấm được nút SAVE** | Chưa cân đủ các bước trước, hoặc chưa quẹt thẻ nhân viên | Kiểm tra danh sách step xem có bước nào bị bỏ sót không; kiểm tra xem đã hiện tên nhân viên trên header chưa. |
| **Báo lỗi "Previous step has not been weighed"** | Đang cố tình chọn cân bước sau trước khi bước trước có dữ liệu | Bấm chọn lại bước phía trước để cân hoàn tất trước. |
| **Trọng lượng hiển thị âm (-) hoặc bất thường** | Cân chưa được Tare về 0 trước khi đặt vật | Bấm nút Zero / Tare trên mặt cân điện tử để đưa về 0.00g rồi cân lại. |
