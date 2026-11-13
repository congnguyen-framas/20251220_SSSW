# 📝 Session: Cân hàng PU (đơn vị PCS-L / PCS-R)

- **Ngày**: 2026-11-10 · **Nhánh**: `feature/use-bom-level-2`

## 1. Yêu cầu
Hàng có `FT601.C016 = 'PU'` (lưu ở `FT600.C019`) cân theo **chiếc (PCS)**, không theo đôi (PRS). Mold / PPIC / Actual Partitioning đọc ra đã là số chiếc; cột Unit (DB `C037` + UI) = `PCS-L` / `PCS-R`.

## 2. Thay đổi
- `FT600.partial.cs`: `IsPu` (C019 = PU, không phải REX), `PuSideOf(stepCode)`, `PuUnitOf(stepCode)`.
- `ShotWeightViewModel`: `ApplyPuUnits` gán `C037` sau khi dựng `_scaleDataFinal`; `UpdateWeightPerPiece` PU → `C036 = C021` (đã là g/chiếc, không chia `C039`); nhãn Partitioning / Adjust và đơn vị Reference panel (`g/pcs`) theo dòng đang chọn (`CountUnit`).
- `PartWeightCalculator.UnitWeightPerPiece`: PU → `C021`.
- `ShotWeightWindow.xaml`: nhãn động; header 3 cột partition ghi `(prs/pcs)`.
- Công thức không đổi: `C021 = C023/N − Σ L2` với N = số chiếc → g/chiếc; đơn vị `PCS-*` khác `PRS` nên không bị nhân/chia 2.

## 2b. Sửa kèm theo (cùng phiên, 2026-11-10 → 11-11) — `ShotWeightViewModel`, `BomService`
- **NullReference `C003`**: chuẩn hóa `C003 ??= ""` sau khi dựng `_scaleData` (và ở dòng Logo group size / REX Level 2); nguyên nhân `ItemStepName` từ SP rỗng hoặc `FT601.C005` null.
- **Step không có dòng FT601 chính xác** (chỉ khớp "mờ"): `C013/C014` lấy theo BOM (`item.ItemFgCode/Name`); bỏ `C017/C018/C019/C020`, `C028 = 0`. Nếu FG có dòng PU thì các step đó gán `C019 = "PU"`. `sameMolds` và `checkMultiSize` bỏ qua dòng mold trống.
- **BOM Level 2**: `BomService.IgnoreNonItemRows` bỏ dòng SP trả thêm không có `ItemStepName` (vd `220280HT`, `CEMENTINGPU`, `PU`).
- **FG/Main của group size và dòng cùng khuôn**: lấy theo `sp_sssw_getproductInfo` (`GetProductInfoAsync` / `ApplyProductInfo`), thiếu thì `CopyMissingFgInfo` từ dòng gốc (Logo group size + sameMolds Studs/Inlay/Cleat_Ring).

## 3. Giả định / việc còn mở
- `C032` và Machine `C004` của step khớp mờ vẫn lấy từ dòng FT601 khớp sai (chưa xử lý); dữ liệu FT600 cũ có FG sai vẫn còn.
- Chưa chạy app thật với bộ dữ liệu FG `6813992601-SG10-F10R`.
- **L/R lấy từ hậu tố mã step** (đoạn sau dấu `-` cuối kết thúc `L`/`R`, vd `-L`, `-2801R`); chưa có mẫu mã thật → cần đối chiếu. Không nhận ra → `PCS`.
- Chưa test với dữ liệu PU thật; chưa áp dụng cho màn FG (`ShotWeightFGViewModel`).

## 4. Cập nhật cuối phiên (2026-11-11)
- Khớp mờ FT601 cho FG PU (`fgIsPu`) phải cùng hậu tố L/R với step; `C032` khi chuyển step khóa phải đúng mã step.
- `SwitchToLockedStepAsync`: step không có trong master vẫn chuyển sang xử lý → `CreateStepFromSelectionAsync` (master FT601 theo mã step, không có thì dựng FT601 tạm + `sp_sssw_getproductInfo`).
- Logo: FG/Main lấy từ `sp_sssw_getproductInfo` (FG name = `ProductName`), lưu Confirm theo FG này; group size PU chỉ giữ size cùng L/R với step gốc; lưu `C019 = PU`, `C037 = PCS-L/PCS-R`.
- Actual / PPIC / Mold pairs-or-pcs shot (`C028/C017/C018`) mặc định 1 khi FT601 không có (null/0).

## 5. UI (2026-11-11)
- Row Details (Total Steps): đơn vị `PCS` cho mọi dòng con (trừ "previous step" = `PRS`); hàng PU giữ đơn vị của child. Header cột `Qty` bỏ `(PCS)`.
- `ShotWeightWindow.xaml`: thêm `GridSplitter` — dọc giữa Total Steps và 2 card dưới (mặc định cao 520, min 200/320), ngang giữa Reference/History và Reference Values (mặc định 50/50, min 320). Kích thước chỉnh tay không được lưu.

## 6. Cập nhật (2026-11-13)
- BOM L2: `ChainPredecessorCodes` chỉ gồm step chain thực sự bị trừ (Studs cùng thứ tự không được chọn vẫn là chi tiết con L2); `ApplyPieceQuantities` chỉ xóa `C039` của step chain được chọn → Studs có Pieces Qty / g-per-piece.
- Lưới Total Steps: đơn vị chuyển từ header vào cell (`ValueUnitConverter`, đơn vị lấy từ `C037`, xuống dòng; Mold Pairs Shot của Logo/Cleat_Ring luôn `pcs`, PU giữ `pcs-L/R`); Total Part header `(g)` và cell không đơn vị; cột Unit ẩn; cột Size wrap (`CenterWrapCell`). Row Details: `Weighed / Weight per piece / Deduct` có đơn vị trong cell.
- Logo hàng PU: Actual Partitioning mặc định 2; group size chỉ lấy size có hậu tố L hoặc R (cả L và R, bỏ size không có L/R).
- `UpdateStepStatuses`: Done chỉ khi đủ thông số (step ép: `C023 > 0` và `C021 > 0`; REX: `C023 > 0`). Trước đây `C024 > 0` làm mọi size cùng khuôn (Logo PU) nhảy Done sau khi cân Total Weight.

## 7. PU nhiều bước theo chân L/R (2026-11-13)
- Hàng PU, bấm Scale / chọn step không phải step cuối, không phải Logo/Cleat_Ring: dựng lại lưới gồm step đó + chân đối xứng L/R (`AddLogoGroupSizesAsync(pairOnly: true)`), cộng step chain ngay trước (cũng đủ L/R); bỏ step sau và step trước không phải chain (đã nằm trong Level 2 của step khác). `NeedsPuPairReload` kích hoạt tải lại khi bấm Scale mà lưới chưa có chân đối xứng.
- Trừ theo chân: `SameSide` — step L trừ component L, step R trừ component R (công thức khuôn nhiều size, `ChainPredecessors`, `FindWeighedChild`).
- Lưới nạp lại giá trị đã cân (FT600) cho mọi step PU đứng trước step đang cân.
- Logo PU: group size lấy size có hậu tố L hoặc R (bỏ size không có L/R).
- Chọn step trong droplist: ưu tiên đúng dòng FT601 đã chọn (`FT601Id`); dòng trống (thiếu `C007`/`C005`) → dùng dòng đầy đủ cùng mã step, tránh NullReference / lưới rỗng (F05R).
