# 🗄️ Database Schema & Data Dictionary

> Tài liệu mô tả chi tiết cơ sở dữ liệu `DbContextDogeWH` (SQL Server) phục vụ hệ thống SSSW.

---

## 1. Tổng quan các Bảng (DbSet)

| Tên Bảng / Entity | Mô tả | Vai trò trong hệ thống |
|-------------------|-------|------------------------|
| **`FT600`** | Kết quả cân Shot Weight | Bảng dữ liệu chính lưu thông tin và khối lượng mỗi lần cân |
| **`FT601`** | Master Step Items (từ Hydra) | Danh sách các bước cân / linh kiện cần thực hiện theo lệnh sản xuất |
| **`FT602`** | Bảng phụ trợ dữ liệu bổ sung | Dữ liệu phụ đi kèm |
| **`FT605`** | Dữ liệu tham chiếu | Master/reference data cho quy trình cân |
| **`FT606_Label`** | Bảng Barcode / Label QR Code | Map mã QR code quét từ tem sang Order/Step/Item |
| **`FT608_Config`** | Cấu hình máy trạm (Machine Config) | Lưu JSON cấu hình phần cứng (cân, barcode, RFID, tolerance) theo `MachineName` |
| **`FT609_ShotWeightUpToWL`** | Dữ liệu đồng bộ lên Winline (WL) | Bảng trung gian log lịch sử sync shotweight sang Winline MES |
| **`FT029_Operator_RFID`** | Danh bạ thẻ RFID công nhân | Map mã thẻ RFID (`C000`) sang Mã nhân viên (`C001`) và Họ tên (`C002`) |
| **`FT031_Department`** | Danh mục phòng ban | Phân loại quyền và phòng ban |

> [!NOTE]
> Tất cả các bảng trên đều kế thừa từ `BaseEntity` và có **Global Query Filter** trong EF Core:
> ```csharp
> modelBuilder.Entity<T>().HasQueryFilter(p => p.Actived == true);
> ```
> Cột `Actived` (bool) quyết định record có hiệu lực hay không.

---

## 2. Chi tiết cấu trúc các bảng trọng yếu

### 2.1. `FT600` — Dữ liệu kết quả cân (Scale Shot Weight)

Mỗi lần công nhân hoàn tất quy trình cân một sản phẩm/bộ linh kiện, hệ thống sẽ chèn 1 hoặc nhiều dòng vào bảng này.

| Cột | Kiểu | Mô tả chi tiết |
|-----|------|----------------|
| `id` | `Guid` (PK) | Định danh duy nhất của lượt cân |
| `C000` | `string` | Location / Machine ID (từ Hydra) |
| `C001` | `EnumSampleLocation` | `Production` (0) hoặc `Sample` (1) |
| `C002` | `string` | Mã Item Step (`ItemStepCode`) — VD: `REX...`, linh kiện con |
| `C003` | `string` | Tên Item Step (`ItemStepName`) — VD: `Outsole`, `Cleat_Ring`, `Inlay` |
| `C004` | `string` | Order No / Production Order |
| `C005` | `string` | Article / Mã sản phẩm chính |
| `C006` | `string` | Model Name / Tên mẫu giày |
| `C007` | `string` | Colorway |
| `C008` | `string` | Size sản phẩm |
| `C009` | `int` | Số lượng mẫu / Số shot |
| `C010` | `string` | Mã thẻ RFID của công nhân thực hiện |
| `C011` | `string` | Mã nhân viên (Employee ID) |
| `C012` | `string` | Mã Barcode / Label đã quét |
| `C013` | `string` | Machine Group / Máy ép |
| `C014` | `string` | Mold ID (Mã khuôn) |
| `C015` | `int?` | `ParallelSequence` (Thứ tự bước / Step sequence) |
| `C016` | `string` | Remark / Ghi chú |
| `C017` | `double?` | Article Pairs per Shot (Số đôi trên 1 shot khuôn) |
| `C018` | `string` | Thông tin phụ từ Hydra |
| `C019` | `string` | Machine Group Code |
| `C020` | `string` | Mold Code |
| **`C021`** | `double?` | **Part Weight (g/prs)** — Trọng lượng chi tiết sau tính toán |
| **`C022`** | `double?` | **Runner Weight (g)** — Trọng lượng cuống nhựa |
| **`C023`** | `double?` | **Part Weight (g/shot)** — Trọng lượng sản phẩm cả shot |
| **`C024`** | `double?` | **Gross Weight (g)** — Tổng trọng lượng cân được (lần 1: Part + Runner) |
| `C025` | `double?` | Standard Weight (Trọng lượng tiêu chuẩn BOM) |
| `C026` | `string` | Hydra Task ID |
| `C027` | `string` | Hydra Batch |
| `C028` | `int` | Số đôi thực tế trên shot |
| `C029` | `Guid?` | ID của `FT606_Label` |
| `C032` | `Guid?` | ID của dòng Hydra tương ứng |
| `C033` | `string` | Category Code của linh kiện |
| `C034` | `string` | Category Name |
| `C035` | `double?` | Tỉ lệ sử dụng (%) `_percentOfUsage` |
| `C036` | `double?` | Trọng lượng lệch so với tiêu chuẩn (Tolerance Delta) |
| `C037` | `string` | Đơn vị tính (g, kg) |
| `AllowScale` | `bool` | Cờ cho phép cân bước này |
| `CreatedDate` | `DateTime` | Ngày giờ cân |
| `CreatedMachine` | `string` | Tên máy trạm thực hiện cân (`Environment.MachineName`) |
| `Mesocomp` | `string` | Mã công ty / Site (`VNT1`, `FTT1`, v.v.) |
| `Mesoyear` | `int` | Năm tài chính Winline |

---

### 2.2. `FT601` — Master Step Items

Chứa danh sách kế hoạch sản xuất và trạng thái đã cân:

| Cột | Kiểu | Ý nghĩa |
|-----|------|---------|
| `C004` | `string` | Order No |
| `C007` | `string` | Step / Item Identifier |
| `C017` | `bool` | **IsScaled** — Đã cân hoàn tất hay chưa (`false`: chờ cân, `true`: đã cân xong) |
| `Actived` | `bool` | Record có hiệu lực |
| `ModifiedDate` | `DateTime?` | Ngày hoàn tất cân |
| `ModifiedMachine` | `string` | Máy trạm hoàn tất |

---

### 2.3. `FT606_Label` — Quản lý Tem nhãn QR Barcode

| Cột | Kiểu | Ý nghĩa |
|-----|------|---------|
| `Id` | `Guid` (PK) | ID nhãn |
| `C000` | `string` | Reference ID liên kết tới Hydra task |
| `C001` | `string` | Barcode string đã in trên tem |
| `Actived` | `bool` | Trạng thái hiệu lực |

---

### 2.4. `FT608_Config` — Cấu hình máy trạm (Machine Config)

| Cột | Kiểu | Ý nghĩa |
|-----|------|---------|
| `id` | `Guid` (PK) | ID |
| `MachineName` | `string` | Tên máy tính Windows chạy ứng dụng (`Environment.MachineName`) |
| `ConfigJson` | `string` | Nội dung JSON deserialized thành class `ConfigModel` |
| `Actived` | `bool` | Hiệu lực |

#### Cấu trúc JSON bên trong `ConfigJson`:
- `Scale`: IP, Port, ModelName, DecimalNum, CalibZero, CalibGain, CheckStable, CheckTare, TimeScan.
- `Barcode`: Cổng kết nối / SDK Scanner.
- `Rfid`: COM Port, BaudRate.
- `Tolerance`: Các ngưỡng sai số tiêu chuẩn theo gram/phần trăm.

---

### 2.5. `FT029_Operator_RFID` — Thẻ công nhân

| Cột | Kiểu | Ý nghĩa |
|-----|------|---------|
| `C000` | `string` | Mã thẻ RFID (quẹt thẻ) |
| `C001` | `string` | Mã nhân viên (Employee ID) |
| `C002` | `string` | Tên nhân viên (Full Name) |
| `Actived` | `bool` | Trạng thái còn làm việc hay không |

---

## 3. Danh sách Stored Procedures (SP) trọng yếu

1. **`sp_MaterialGetCompanyName`**: Trả về `mesocomp` (VNT1, FTT1...) để xác định site hiện tại.
2. **`sp_MaterialGetMesoyear`**: Trả về năm làm việc (`mesoyear`) hiện hành trong Winline.
3. **`sp_getBomWinlineOfItemFG`**: Truy vấn định mức BOM từ Winline cho mã thành phẩm (Finished Goods).
4. **`sp_GetFullStepItemHydraIsRun`**: Lấy danh sách các lệnh sản xuất / khuôn đang chạy trên máy ép từ Hydra MES.
5. **`sp_GetCategorryOfItem`**: Lấy danh mục nhóm linh kiện.
6. **`sp_Shotweight_Gets`**: Lấy thông tin tiêu chuẩn shotweight từ Winline để đối chiếu.
7. **`sp_Showweight_UpdateToWinline`**: Cập nhật kết quả shotweight đã cân ngược lại database Winline.
