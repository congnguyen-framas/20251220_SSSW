# ⚖️ Business Logic & Calculation Formulas

> Tài liệu mô tả toàn bộ logic nghiệp vụ, quy trình cân, công thức tính toán và các quy tắc đặc biệt trong `ShotWeightViewModel` và `ShotWeightFGViewModel`.

---

## 1. Các thuật ngữ & Khái niệm cốt lõi

- **Shot Weight**: Tổng khối lượng một lần ép nhựa của khuôn (bao gồm các sản phẩm/linh kiện ra khỏi khuôn + cuống nhựa dẫn hướng gọi là runner).
- **Part Weight**: Khối lượng của riêng linh kiện/sản phẩm (tính trên 1 đôi `g/prs` hoặc trên cả shot `g/shot`).
- **Runner Weight**: Khối lượng cuống nhựa (sprue/runner).
- **Step / Sequence**: Thứ tự ép hoặc lắp ghép (Step 1, Step 2...). Trong khuôn nhiều bước (multi-stage injection) hoặc khuôn nhiều màu (bi-color / multi-component), các bước trước phải được cân trước bước sau.

---

## 2. Các trường khối lượng chính (`C021`, `C022`, `C023`, `C024`)

| Cột | Tên trên UI | Ý nghĩa | Công thức cơ bản |
|-----|-------------|---------|------------------|
| **`C024`** | **Total / Gross Weight (g)** | Tổng khối lượng đo được ở lần cân đầu | Đọc trực tiếp từ cân khi đặt cả shot (Part + Runner) |
| **`C022`** | **Runner Weight (g)** | Trọng lượng cuống nhựa | Đọc từ cân ở lần cân thứ hai (chỉ đặt runner) |
| **`C023`** | **Part (g/shot)** | Trọng lượng toàn bộ sản phẩm trong 1 shot | `= C024 - C022` (Gross - Runner) |
| **`C021`** | **Part (g/prs)** | Trọng lượng tính trên 1 đôi sản phẩm | `= C023 / (Số đôi trên shot * % Usage)` |

---

## 3. Quy trình cân 2 bước chuẩn (Standard Injection Items)

Dành cho các linh kiện thông thường ép nhựa:

```
[Bắt đầu lượt cân]
       │
       ▼
[Lần cân 1: Đặt cả Shot (Part + Runner) lên cân]
       │
       ├─► UI hiển thị giá trị ổn định từ cân
       ├─► Bấm nút [SCALE] hoặc tự động đọc
       └─► Ghi nhận: C024 = Weight_Scale1
       │
       ▼
[Lần cân 2: Bỏ sản phẩm ra, CHỈ ĐẶT RUNNER lên cân]
       │
       ├─► Bấm nút [SCALE]
       ├─► Ghi nhận: C022 = Weight_Scale2 (Runner)
       ├─► Tự động tính C023: C023 = C024 - C022
       └─► Tự động tính C021: xem §4.8 (Logo/Cleat_Ring: C021 = C023; step ghép: C023 − Σ chi tiết con theo BOM Level 2)
       │
       ▼
[Hoàn tất 1 Step]
```

---

## 4. Các trường hợp xử lý đặc biệt (Special Business Rules)

### 4.1. Linh kiện REX / Cleat (Đinh sắt chèn sẵn)
- Mã item bắt đầu bằng `"REX"` (ví dụ đinh tán, insert kim loại).
- **Quy tắc**: REX không có cuống nhựa (Runner). Do đó **chỉ cần cân 1 lần**:
  - `C024 = Weight_Scale`
  - `C022 = 0` (không có runner)
  - `C023 = C024`
  - `C021 = C023 / PairsPerShot`
  - Hoàn tất bước ngay sau 1 lần cân.

### 4.2. Linh kiện `Cleat_Ring` (Vòng đệm bao quanh đinh)
- Khi ép nhựa bao quanh đinh REX (`Cleat_Ring`), tổng khối lượng đo được của cụm bao gồm cả phần đinh REX đã chèn vào từ trước.
- **Công thức Cascade**:
  ```
  Khối lượng nhựa thực của Cleat_Ring = Tổng cân được - Khối lượng đinh REX đã cân ở bước trước
  ```
- **Lưu ý kiểm tra logic (Fix ngày 2026-08-18)**:
  Filter phải khớp đúng chuỗi `StartsWith("Cleat_Ring")` để tránh ghi đè nhầm hoặc tính ra giá trị âm (`C021 < 0`).

### 4.3. Linh kiện `Inlay`
- Tương tự như insert kim loại/vải chèn trong lòng khuôn. Kể từ 2026-11-02, Part Weight của Inlay (và Studs/Base) được tính theo **BOM Level 2** (xem §4.8), không còn nhánh đặc cách theo tên step.

### 4.4. Kiểm tra thứ tự tuần tự (Sequential Step Validation)
- Từ 2026-11-04: **không còn khóa nút cân theo loại step** (`AllowScale` luôn `true`) và **không còn bắt cân hết mọi bước trước**.
- Step có BOM Level 2: chỉ cần các **chi tiết con** (§4.8) đã có trọng lượng. Thiếu chi tiết nào, lúc Save hệ thống cảnh báo
  `"Component step(s) of this step (BOM level 2) have no weight yet and must be weighed first"` kèm danh sách và từ chối ghi.
- Step **không có** Level 2 (SP rỗng/lỗi) và không phải nonwoven: **Part Weight = chính nó** (`C023 / N`), không trừ bước nào và không phụ thuộc bước trước. Step có Level 2 mà không có chi tiết con thì cũng bằng chính nó.

> **Nhiều đôi cùng size trong 1 shot (Actual Partitioning N)**: số cân lần 2 S là tổng part của N đôi → `C023 = S` (giữ nguyên, không chia N), `C021 = C023/N − Σ level 2` (D tính cho 1 đôi), `C022 = (C024 − S)/N` (Runner = YES, ngược lại 0); N rỗng/0 coi là 1. Ví dụ W=100, S=80, N=2 → C023=80, C021=40, C022=10.

> **Ngoại lệ cách tính Part Weight**: chỉ step thuộc 1 khuôn nhiều size hoặc có vải nonwoven/mesh dùng công thức cũ (C023/N − Σ C021 các bước trước − Σ C021 REX non-injection cùng step), và phải cân xong mọi bước trước (popup `"Previous step(s) are missing Part Weight and must be weighed first."`). Cascade các bước sau của nhóm này cũng theo cách cũ; phần chia lại runner giữa các size khi Save giữ nguyên. REX không phụ thuộc bước trước.
- Chi tiết: [`ALLOWSCALE_LOGIC_REVIEW.md`](ALLOWSCALE_LOGIC_REVIEW.md).

### 4.5. Khuôn nhiều Size trên cùng 1 Shot (Multi-size Molds)
- Cùng 1 khuôn ép ra nhiều cỡ giày khác nhau trong 1 lần ép:
  - Hệ thống gom nhóm theo tiền tố mã hàng (`PrefixUpToThirdHyphen`), tính tổng số đôi và phân bổ trọng lượng theo tỉ lệ định mức BOM tương ứng từng size.

---

### 4.8. Part Weight theo BOM Level 2 (từ 2026-11-02)

Thay cho cơ chế cũ "trừ Part Weight của mọi bước trước". Áp dụng cho injection step ở **lần cân thứ hai** (trừ Logo/Cleat_Ring vẫn `C021 = C023`).

```
C021(step) = C023(step) − Σ over child in L2(step), child ≠ self :  Qty(child) × UnitWeight(child)
```

- `L2(step)` = `sp_getBomWinlineOfItemFG @itemFG = <ItemStepCode của step>` (cache theo phiên, `IBomService`).
- Dòng **self**: mã kết thúc `-0000` / đơn vị `G` / trùng mã step → không trừ.
- `Qty` ở L2 là **số chiếc (PCS)** của chi tiết con trong 1 đôi của step cha. `UnitWeight` (g/chiếc) = `C036` đã lưu của chi tiết con;
  nếu chưa có (không phải REX) thì `C021 ÷ C039`. `C039` = **số chiếc của chi tiết đó trong 1 đôi, gộp theo toàn FG**:
  `C039(c) = Σ qty(step cha → c)` (bỏ dòng self); step không là chi tiết của step nào (vd. step cuối) thì lấy Quantity Level 1 quy ra chiếc (1 PRS = 2 PCS).
  Ví dụ FG `6112042313-A478-2801`: Studs = 8 (Inlay) + 2 (Base_FF) = **10 chiếc** → cân 22.10 g/prs ⇒ **2.21 g/chiếc**; Logo = 2; Inlay = 2; Base_FF = 2; REX00018 = 8 + 4 = 12.
  Cột `C025` (Quantity, Level 1) giữ nguyên; `C039` là cột mới.
- Trọng lượng/chiếc `C036` được cập nhật mỗi lần cân/tính lại `C021`: `C036 = C021 ÷ C039` (REX Receptacle: `scale ÷ C039`).
- REX chỉ có trong Level 2 (không có dòng ở Level 1) được đưa ra lưới thành dòng riêng để cân: một dòng cho mỗi mã REX, `C015 = 0` (REX có thể thuộc nhiều step), đặt đầu danh sách, `C025 = 1`, cân đủ số chiếc của 1 đôi = tổng qua các step (`C039`).
- Chi tiết con lấy từ dòng cùng phiên (`_scaleDataFinal`) trước, nếu không có thì FT600 mới nhất theo `C002`.
- Chi tiết con chưa cân (hoặc trọng lượng ≤ 0) → cảnh báo, **không ghi** `C023`/`C021`.
- Không có L2 (SP rỗng/lỗi, `Z-VHXXXXXX`) → rơi về công thức cũ (`ApplyLegacyPartWeight`).
- **Cascade**: khi một step đổi, các step cha đã cân có step đó trong L2 được tính lại `C021` và `C022 = C024 − C021`.

**Cây BOM của FG `6112042313-A478-2801`** (Level 1 → Level 2; số = Qty):

```mermaid
graph TD
    FG["FG 6112042313-A478-2801"]
    FG --> S0["0-…-2706 Logo"]
    FG --> S1["1-…-5065 Studs"]
    FG --> S2["2-…-2801 Inlay"]
    FG --> S3["3-…-2801 Base_FF"]
    FG --> S4["4-…-2801 Base_HF"]

    S2 -. "×8" .-> S1
    S3 -. "×2" .-> S1
    S3 -. "×2" .-> S2
    S3 -. "×8 PCS" .-> R["REX00018 Receptacle"]
    S4 -. "×2" .-> S0
    S4 -. "×4 PCS" .-> R
    S4 -. "×2" .-> S3
```

Nét đứt = quan hệ Level 2 (chi tiết con bị trừ khi tính `C021` của step cha). Base_HF **không** chứa Studs/Inlay trực tiếp nên không trừ chúng.

**Luồng quyết định khi bấm Save (lần cân 2)**:

```mermaid
flowchart TD
    A([Save lần cân 2: C023 = scale]) --> B{Logo hoặc Cleat_Ring?}
    B -- Có --> B1["C021 = C023<br/>C036 = scale / C025"]
    B -- Không --> C{"Có BOM Level 2<br/>của step?"}
    C -- Không --> L["ApplyLegacyPartWeight<br/>(công thức cũ)"]
    C -- Có --> D["Calculator: duyệt từng chi tiết con<br/>(bỏ dòng self)"]
    D --> E{"Chi tiết con<br/>đã có trọng lượng?"}
    E -- Chưa --> F["Cảnh báo + reset C023/C022<br/>KHÔNG lưu"]
    E -- Đủ --> G["C021 = C023 − Σ Qty × UnitWeight"]
    G --> H["CascadeLevel2:<br/>tính lại step cha đã cân"]
    B1 --> Z([RefreshUI / chuyển step kế tiếp])
    L --> Z
    H --> Z
```

**Trình tự gọi (sequence)**:

```mermaid
sequenceDiagram
    actor Op as Operator
    participant VM as ShotWeightViewModel
    participant BOM as IBomService
    participant Repo as IScaleStepRepository
    participant Calc as IPartWeightCalculator
    participant DB as SQL Server

    Op->>VM: Quét tem / chọn step
    VM->>BOM: GetLevel1Async(FG)
    BOM->>DB: sp_getBomWinlineOfItemFG @itemFG=FG
    loop mỗi step ép trong danh sách
        VM->>BOM: GetLevel2Async(stepCode)
        BOM->>DB: sp_getBomWinlineOfItemFG @itemFG=stepCode (cache)
    end
    VM->>Repo: GetLatestByStepCodesAsync(mã chi tiết con)
    Repo->>DB: SELECT FT600 mới nhất theo C002
    Op->>VM: Save (lần cân 2)
    VM->>Calc: Calculate(C023, stepCode, L2, FindWeighedChild)
    Calc-->>VM: PartWeightResult (C021 hoặc MissingChildren)
    alt thiếu chi tiết con
        VM-->>Op: Cảnh báo, không ghi
    else đủ
        VM->>VM: C021 = result; CascadeLevel2()
    end
```

**Trọng lượng 1 chiếc `UnitWeight`** (không còn giả định 1 đôi = 2 chiếc):

```mermaid
flowchart LR
    W["FT600 của chi tiết con"] --> S{"C036 > 0?"}
    S -- Có --> S1["UnitWeight = C036 (g/chiếc)"]
    S -- Không --> R{"REX?"}
    R -- Có --> R1["Chưa có trọng lượng → thiếu chi tiết"]
    R -- Không --> P{"C021 > 0 và C039 > 0?"}
    P -- Có --> P1["UnitWeight = C021 / C039"]
    P -- Không --> P2["Chưa có trọng lượng → thiếu chi tiết"]
```

Ví dụ (FG `6112042313-A478-2801`): Base_FF = self + Studs×2 + Inlay×2 + REX00018×8; Base_HF = self + Logo×2 + REX00018×4 + Base_FF×2.

**Không đổi**: nhánh REX/Nonwoven/Mesh (§4.1), multi-size (§4.5), `C022`/`C023`/`C024`, `C036` của Logo/Cleat_Ring.
Code: `UI/WPF/Services/PartWeightCalculator.cs`, test: `tests/SSSW.Tests`.

---

## 5. Quy tắc Đăng nhập & Xác thực Thẻ RFID

1. **Bắt buộc có danh tính Operator trước khi Lưu**:
   - Khi bấm **[SAVE]**, kiểm tra `OperatorSession.IsLoggedIn` hoặc thông tin `FT029`.
   - Nếu chưa quẹt thẻ RFID:
     - Hệ thống yêu cầu quẹt thẻ hoặc mở dialog `frmRfidInput` để nhập mã nhân viên thủ công.
     - Sau khi xác nhận thành công, gán `C010` (Mã RFID) và `C011` (Mã NV) vào bản ghi.
2. **Session dùng chung (Singleton OperatorSessionService)**:
   - Một khi đã quẹt thẻ trên màn hình Main, phiên đăng nhập được duy trì cho cả tab Step Component và Finished Goods cho đến khi bấm nút Đăng xuất / Quét thẻ mới.

---

## 6. Luồng Lưu & Commit Transaction (`ExecuteSave`)

1. **Validate**:
   - ~~Kiểm tra `AllowScale == true`~~ (đã bỏ khóa 2026-11-04; `AllowScale` luôn `true`).
   - Kiểm tra các bước trước đã hoàn tất.
   - Kiểm tra đã có thẻ RFID.
2. **Database Transaction**:
   - Mở Transaction EF Core: `await db.Database.BeginTransactionAsync()`.
   - `INSERT` danh sách dòng hợp lệ (`C021 > 0`) vào bảng `FT600`. **Mọi dòng insert** (trừ dòng cùng khuôn khác size) **thay `C013` (FG code) và `C014` (FG name) theo FG của step đang scan/chọn** (dòng khớp Hydra mờ không còn mang FG của lệnh khác). Step không có dòng Hydra đúng mã: `C005` (article) = mã step bỏ đoạn màu, `C027` trống = tên step bỏ loại step và màu.
   - `UPDATE` cờ hoàn tất trên `FT601`: `C017 = true` cho các lệnh tương ứng.
   - `COMMIT` transaction.
3. **Đồng bộ Winline (Background Task / Optional)**:
   - Gọi `sp_Showweight_UpdateToWinline` nếu cấu hình tự động đẩy.
4. **Reset UI**:
   - Reset form về trạng thái sẵn sàng cho lượt quét mã / lệnh tiếp theo.
