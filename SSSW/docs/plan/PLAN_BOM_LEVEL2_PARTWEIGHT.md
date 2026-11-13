# Brainstorm & Plan: Tính Part Weight theo BOM Level 2

- **Nhánh**: `feature/use-bom-level-2` (từ `main`)
- **Trạng thái**: ⏳ CHỜ DUYỆT — chưa sửa code nào
- **Phạm vi**: `ShotWeightViewModel` (màn cân Step Component). `ShotWeightFGViewModel` không đụng.

---

## 1. Hiện trạng (đã khảo sát code)

### 1.1 Cách tính `C021` hiện tại ([ShotWeightViewModel.cs:1213-1415](../../UI/WPF/ViewModels/ShotWeightViewModel.cs))

Lần cân 2 của injection item: `C023 = scale`, rồi

```
C021 = C023 − Σ C021(mọi step có C015 < step hiện tại) − Σ C021(REX / Z-VHXXXXXX cùng C015)
```

Chồng lên đó là các nhánh đặc cách theo **tên** step (`Inlay`, `Studs`, `Logo`, `Cleat_Ring`) và một đoạn hack ghi rõ *"tạm thời… sau này khi sửa công thức BOM theo level 2 thì bỏ"* ([dòng 1277](../../UI/WPF/ViewModels/ShotWeightViewModel.cs)). Sau đó còn khối **Cascade** (dòng 1392-1415) tính lại `C021/C022` của các step sau theo chuỗi `C023`.

Vấn đề gốc: "trừ hết các bước trước" chỉ đúng khi BOM là một chuỗi tuyến tính. Ảnh BOM cho thấy không phải vậy:

| Step (L1) | Cấu thành thực tế (L2) |
|---|---|
| `2-…-2801` Inlay | self + **Studs ×8** |
| `3-…-2801` Base_FF | self + **Studs ×2** + **Inlay ×2** + **REX00018 ×8** |
| `4-…-2801` Base_HF | self + **Logo ×2** + **REX00018 ×4** + **Base_FF ×2** |

Base_HF **không** chứa Studs/Inlay trực tiếp, còn Base_FF thì có → công thức "trừ tất cả bước trước" sai cho cả hai, nên mới phải vá bằng các nhánh theo tên.

### 1.2 Phần phải GIỮ NGUYÊN 100% (Zero Logic Mutation)

| Logic | Vị trí | Ghi chú |
|---|---|---|
| Nhánh non-injection (REX) trong `ExecuteSave` | dòng 1342-1371 | Receptacle: `C024=C023=C021=scale`, `C036=scale/C025`. Nonwoven/Mesh (`catCheck != null`): `usage = scale×%/100/C028`, `unusage`, `C036=usage/2` |
| `OnUsagePctEnter` tính lại REX theo % usage | dòng 1821-1850 | |
| Nạp lại REX từ `FT600` cũ (dùng `C036`, `C035`) | dòng 1114-1129 | |
| Nhân bản dòng REX cho từng size khi Confirm | dòng 1484-1560 | multi-size mold |
| Multi-size: chia `C024`, tính `C022` theo `Σ C023×C028` | dòng 1373-1390 | |
| Lần cân 1 (`C024`), `C022` (runner, YES/NO), `C023`, `C036` của Logo/Studs/Cleat_Ring | dòng 1244-1262, 1329-1331 | |
| Validate bước trước phải có `C021 > 0` | dòng 1224-1239 | |
| Bộ lọc AllowScale / RemoveAll theo Logo/Studs/Ring/Inlay | dòng 898-1055 | vừa sửa ở session 2026-10-30, không đụng |

→ **Chỉ thay đúng một thứ**: cách ra `C021` của injection item (và khối Cascade phụ thuộc nó).

---

## 2. Thiết kế cơ chế mới

### 2.1 Công thức

```
C021(step) = C023(step) − Σ over children c in L2(step), c ≠ self :  Qty(c) × UnitWeight(c)
```

- `L2(step)` = kết quả `sp_getBomWinlineOfItemFG @itemFG = <ItemStepCode của step>` (đúng như ảnh). Không cần stored procedure mới.
- Dòng **self** (`ItemStepCode` kết thúc `-0000`, Unit = `G`, Qty 1) = chính phần nhựa của step → **loại khỏi tổng trừ**.
- Child khớp với `FT600.C002` của step đã cân (trong phiên hiện tại `_scaleDataFinal`, nếu không có thì lấy bản ghi mới nhất trong FT600 — giống cách đang nạp "stepPrevious").
- Step không có child nào (Logo, Cleat_Ring, Studs đầu chuỗi) → `C021 = C023`. **Tự nhiên ra đúng, bỏ được toàn bộ nhánh theo tên + đoạn hack tạm.**

### 2.2 Đơn vị (điểm rủi ro số 1)

Bạn nói đơn vị trong L2 đều là PCS (dù cột Unit trong ảnh hiển thị lẫn PRS/PCS/G). Giả định mình đề xuất — **cần bạn xác nhận**:

- `Qty` ở L2 luôn hiểu là **số chiếc (piece)** trên 1 đôi của step cha.
- `UnitWeight(c)` = **g/piece** của child:
  - Logo, Studs, Cleat_Ring, REX: đã có sẵn `C036` (g/piece) → dùng thẳng.
  - Inlay, Base (child là step ép): `C021 / 2` (1 đôi = 2 chiếc), cùng quy ước với `C036 = usage/2` đang dùng cho Nonwoven.
- REX Nonwoven/Mesh: vẫn qua `C036` nên không đổi logic.

Ví dụ Base_FF: `C021 = C023 − (2×Studs.C036 + 2×Inlay.C021/2 + 8×REX.C036)`.

### 2.3 Cascade

Khi cân lại một step đã có step phía sau, tính lại `C021` (và `C022 = C024 − C021` như cũ) của các step cha có child trùng step vừa đổi — duyệt theo thứ tự `C015`, mỗi step gọi cùng hàm tính L2. Thay cho khối Cascade `C023 − prev.C023` hiện tại.

---

## 3. Test an toàn: bảng `FT600_dev`

```sql
-- Cấu trúc + dữ liệu (cần dữ liệu để các step cũ được nạp lại như production)
SELECT * INTO dbo.FT600_dev FROM dbo.FT600;
-- Sau đó tạo lại PK/default/index giống FT600 (script bằng SSMS → Generate Scripts, đổi tên).
```

- Thêm khóa cấu hình (vd. `ConfigModel.UseDevTables` hoặc `appsettings` `Dev:Ft600Table`); `DbContextDogeWH.OnModelCreating` gọi `ToTable(...)` theo khóa đó → **một bản build, đổi bảng bằng config**, không hardcode.
- Title bar hiện `[DEV]` khi bật để không nhầm.
- ⚠️ `sp_ShotweightReport_UpsertFT609` (chạy khi cân bước cuối) có thể đọc thẳng `FT600`. Ở chế độ dev: **tạm bỏ qua lời gọi upsert** (hoặc cần bản sp dev) — cần bạn chọn.

---

## 4. Chuẩn hóa MVVM

`ShotWeightViewModel.cs` đang 2098 dòng gồm UI state + truy vấn DB + tính toán + lọc step + `MessageBox.Show` (WinForms) ngay trong VM. Đề xuất tách, **mỗi bước giữ hành vi y nguyên**:

| Thành phần mới | Trách nhiệm | Lấy từ |
|---|---|---|
| `Services/IPartWeightCalculator` | Hàm thuần: L2 + danh sách FT600 → `C021`. Không DB, không UI → test được | `ExecuteSave` + Cascade |
| `Services/IBomService` | `GetLevel1/Level2(itemCode)`, cache theo phiên | `GetDataAsync` (sp_getBomWinline…) |
| `Services/IScaleStepRepository` | Đọc/ghi FT600, FT601, chọn bảng `FT600`/`FT600_dev` | `db.FT600s…` rải rác |
| `Services/IDialogService` | Thay `System.Windows.Forms.MessageBox` trong VM | ~10 chỗ |
| `Services/IStepFilterService` | Logic AllowScale / RemoveAll (di chuyển nguyên văn) | dòng 898-1055 |
| `ShotWeightViewModel` | Chỉ state, command, gọi service | còn lại |

Đăng ký DI trong `Program.cs`. Code-behind giữ nguyên vai trò (Win32/DevExpress/driver bridge).

> Dự án chưa có test project. Đề xuất thêm `SSSW.Tests` (xUnit) **chỉ cho `PartWeightCalculator`** với số liệu từ ảnh BOM — cần bạn đồng ý vì thêm project vào solution.

---

## 5. Kế hoạch triển khai (mỗi bước build Debug + commit riêng)

1. **DB dev**: script `FT600_dev`, khóa cấu hình, `ToTable`, badge `[DEV]`.
2. **Tách service không đổi hành vi** (Dialog, Repository, BomService, StepFilter) — refactor thuần, so sánh kết quả cân với bản cũ trên `FT600_dev`.
3. **`PartWeightCalculator` + test** theo công thức §2, chạy song song log so sánh `C021` cũ vs mới (chưa thay thế).
4. **Chuyển `ExecuteSave` + Cascade** sang calculator; xóa nhánh theo tên + đoạn hack Inlay/Studs; **không đụng** nhánh REX/Nonwoven.
5. **Kiểm thử thực tế** trên `FT600_dev`: FG `6112042313-A478-2801` (4 step ảnh), multi-size mold, REX Receptacle, Nonwoven/Mesh, cân lại step giữa chuỗi.
6. Cập nhật `docs/tech/BUSINESS_LOGIC.md` (§3, §4.2-4.3), thêm session log + worklog.

---

## 6. Cần bạn quyết định trước khi code

1. **Đơn vị** (§2.2): đồng ý "Qty = số chiếc, UnitWeight = `C036` hoặc `C021/2`"? Nếu khác, cho mình 1 ví dụ số thật (scale → C021 mong đợi) của Base_FF để đối chiếu.
2. **Child chưa cân / không tìm thấy** trong FT600: chặn lưu + báo lỗi (đề xuất, nhất quán rule hiện tại) hay coi bằng 0?
3. **UpsertFT609 ở chế độ dev**: bỏ qua hay dùng sp dev riêng?
4. **Thêm project test xUnit**: có/không?
5. **Phạm vi refactor MVVM**: làm cả bước 2 như trên, hay chỉ tách `Calculator` + `Dialog` + `Repository` trước (ít rủi ro hơn), phần còn lại để PR sau?
6. Dữ liệu cũ đã lưu bằng công thức cũ: **không migrate** (chỉ áp dụng cho lần cân mới) — đồng ý?
