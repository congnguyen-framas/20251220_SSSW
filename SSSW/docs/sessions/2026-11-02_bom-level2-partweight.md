# 📝 Session: Part Weight theo BOM Level 2 + chuẩn hóa MVVM

- **Ngày**: 2026-11-02 · **Nhánh**: `feature/use-bom-level-2`
- **Plan đã duyệt**: [`../plan/PLAN_BOM_LEVEL2_PARTWEIGHT.md`](../plan/PLAN_BOM_LEVEL2_PARTWEIGHT.md)

## Thay đổi
- **Công thức mới** (injection step, lần cân 2): `C021 = C023 − Σ(Qty × trọng lượng 1 chiếc của chi tiết con L2)`, bỏ dòng self (`-0000`, đơn vị G). Hàm thuần ở `UI/WPF/Services/PartWeightCalculator.cs`.
  - Trọng lượng 1 chiếc: REX → `C036`; còn lại → `C021` (g/prs) chia 2 nếu `C037 == PRS`.
  - Chi tiết con chưa cân → cảnh báo, không ghi `C023`/`C021`.
  - Không có L2 (SP trả rỗng/lỗi, `Z-VHXXXXXX`) → rơi về công thức cũ (`ApplyLegacyPartWeight`).
  - Cascade: `CascadeLevel2()` tính lại step cha đã cân khi chi tiết con đổi; khi không có L2 vẫn chạy cascade cũ.
- **Giữ nguyên**: nhánh REX Receptacle/Nonwoven/Mesh, `OnUsagePctEnter`, multi-size, C036 Logo/Cleat_Ring, lọc AllowScale.
- **MVVM**: thêm `IDialogService` (thay 19 lời gọi `MessageBox` trong VM), `IBomService` (L1/L2 + cache), `IScaleStepRepository`, `IPartWeightCalculator`; đăng ký DI trong `Program.cs`.
- **Test**: entity `FT600` tạm trỏ `[Table("FT600_dev")]` (bảng do người dùng tự copy). **Phải trả về `"FT600"` trước khi release.**
- Project `tests/SSSW.Tests` (xUnit) cho calculator, đã thêm vào `SSSW.sln`.

## Chưa làm / lưu ý
- `StepFilterService` (tách logic AllowScale) và `ScaleStepRepository` đầy đủ (Confirm/History) để PR sau.
- Test chưa chạy được trên máy này: `dotnet restore` timeout vì NuGet source nội bộ `10.40.10.4:5860` không truy cập được.
- `sp_ShotweightReport_UpsertFT609` vẫn đọc bảng FT600 thật khi cân bước cuối trong lúc test trên FT600_dev.
- Quy ước đơn vị (PRS = 2 chiếc) cần đối chiếu với số liệu cân thật.
- Build Debug: 0 error.
