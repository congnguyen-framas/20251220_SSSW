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
