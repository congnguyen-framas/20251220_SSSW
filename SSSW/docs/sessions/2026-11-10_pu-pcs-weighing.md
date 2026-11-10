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

## 3. Giả định / việc còn mở
- **L/R lấy từ hậu tố mã step** (đoạn sau dấu `-` cuối kết thúc `L`/`R`, vd `-L`, `-2801R`); chưa có mẫu mã thật → cần đối chiếu. Không nhận ra → `PCS`.
- Chưa test với dữ liệu PU thật; chưa áp dụng cho màn FG (`ShotWeightFGViewModel`).
