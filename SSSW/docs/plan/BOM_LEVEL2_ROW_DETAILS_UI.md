# Thiết kế UI: BOM Level 2 Row Details trong TOTAL STEPS

- **Ngày**: 2026-11-04 · **Nhánh**: `feature/use-bom-level-2`
- **Trạng thái**: ⏳ **Chờ review** hình v4 (đã vẽ lại theo đúng bộ cột thật của lưới). Mã nguồn bản v3 đã có sẵn trên nhánh — chỉ số cột trong code (0–9) khớp bảng §3 nên sẽ không phải sửa nếu bạn duyệt; chưa code thêm gì theo yêu cầu này
- **Liên quan**: [`brainstorm/BOM_LEVEL2_UI_OPTIONS.md`](../brainstorm/BOM_LEVEL2_UI_OPTIONS.md) (các phương án), [`tech/BUSINESS_LOGIC.md`](../tech/BUSINESS_LOGIC.md) §4.8 (công thức `C021`), session [`2026-11-04`](../sessions/2026-11-04_status-column-align-bom-l2-ui-options.md)

## 1. Yêu cầu

1. Chọn phương án A (mở rộng dòng), thiết kế lại: chọn step nào thì **highlight** và **làm mờ** các dòng còn lại; **bỏ** tiêu đề "BOM LEVEL 2…"; click dòng lần nữa thì về bình thường.
2. Bỏ dòng *self* trong bảng chi tiết.
3. Chuyển toàn bộ cột và mô tả sang tiếng Anh.
4. **Sắp lại thứ tự cột chi tiết (số xanh) và căn cùng đường dọc với cột lưới chính (số đỏ).**

## 2. Hình thiết kế (v4 — theo đúng lưới thật)

![Option A v4](../image/bom_level2_option_A_v4_real_columns.png)

- Lưới TOTAL STEPS thật có **nhiều cột** (hơn 20, kéo ngang bằng thanh cuộn): Actions, STATUS, MACHINE, MAIN / MAIN CODE, Step No., STEP / STEP CODE, SIZE, TOTAL WEIGHT, TOTAL PART, PART, RUNNER / EXCESS MAT., Actual Partitioning, PPIC Partitioning, Mold Pairs Shot, Quantity, … Hình v4 vẽ đủ các cột này (đường kẻ ngang cắt ở Quantity giống màn hình thật).
- Đường gióng đỏ nét đứt = ranh giới cột lưới chính; cột chi tiết nằm trọn trong các ranh giới đó.
- Dải chi tiết rộng bằng cả lưới và **cuộn ngang cùng lưới**; các cột bên phải cột PART (RUNNER … Quantity và các cột xa hơn) để trống.
- Dữ liệu trong hình là minh họa (bố cục lấy từ ảnh chụp lưới thật, FG `6112042502`; Qty/trọng lượng chi tiết do mình đặt ví dụ).

## 3. Ánh xạ cột chi tiết → cột lưới chính

**Chỉ số cột thật của `dgTotalSteps`**: 0 Actions · 1 STATUS · 2 MACHINE · 3 MAIN / MAIN CODE · 4 Step No. · 5 STEP / STEP CODE · 6 SIZE · 7 TOTAL WEIGHT · 8 TOTAL PART · 9 PART · 10 RUNNER / EXCESS MAT. · 11 Actual Partitioning · 12 PPIC Partitioning · 13 Mold Pairs Shot · 14 Quantity · 15 Weight per Unit · 16 Unit · 17 FG / FG CODE · … (còn các cột MACHINE GROUP, MOLD ID, REMARKS, ARTICLE, HYDRA ORDER, CATEGORY, Usage %…).

| # | Cột chi tiết | Cột lưới chính (index trong `dgTotalSteps`) | Rộng (px) | Nguồn dữ liệu |
|---|--------------|---------------------------------------------|-----------|---------------|
| 1 | Component | Actions (0) | 125 | `ItemStepName` của BOM L2 |
| 2 | Status | STATUS (1) | 65 | đã cân / chưa cân |
| 3 | Step | MACHINE (2) | 75 | `C015` của chi tiết con, `REX` nếu non-injection |
| — | *(trống)* | MAIN / MAIN CODE (3) | 150 | — |
| 4 | Qty (PCS) | Step No. (4) | 80 | `Quantity` BOM L2 |
| 5 | Unit | STEP / STEP CODE (5) | 300 | `Unit` BOM L2 |
| — | *(trống)* | SIZE (6) | 60 | — |
| 6 | Weighed (g/prs) | TOTAL WEIGHT (7) | 100 | `C021` đã cân |
| 7 | g / piece | TOTAL PART (8) | 60 | `UnitWeightPerPiece` |
| 8 | Deduct (g) | PART (9) | 60 | `Qty × g/piece` |
| — | *(trống)* | RUNNER / EXCESS MAT. (10) → Quantity (14) và mọi cột xa hơn | 100 / 100 / 100 / 80 / 70 / … | — |

> Lưu ý: lưới thật đã có sẵn các cột **Quantity (14)**, **Weight per Unit (15)**, **Unit (16)** nằm xa bên phải. Phương án này không dùng chúng cho chi tiết con vì chúng thuộc về chính step đang cân; chi tiết con dùng Qty/Unit riêng của BOM Level 2 đặt ở các cột nhìn thấy ngay (Step No., STEP / STEP CODE).

## 4. Tính khả thi

**Khả thi — đã triển khai.** Lý do và điểm cần lưu ý:

| Vấn đề | Giải pháp | Ghi chú |
|--------|-----------|---------|
| Cột chi tiết phải thẳng hàng với cột lưới | Mỗi ô là `Border` có `Width` bind `Columns[i].ActualWidth` của `DataGrid` (`RelativeSource AncestorType=DataGrid`) | Kéo đổi độ rộng cột vẫn thẳng hàng. Không dùng `ColumnDefinition` vì `RelativeSource` không hoạt động ở đó |
| Lưới thật có nhiều cột hơn các mockup trước (v1–v3 đã lược bớt) | v4 vẽ đủ bộ cột thật; MAIN / MAIN CODE, SIZE và các cột từ RUNNER trở đi để trống | Nếu thêm/bớt/đổi thứ tự cột lưới thì phải sửa chỉ số trong `Level2DetailsTemplate` |
| Lưới có thanh cuộn ngang | Dải chi tiết là một phần của `RowDetailsTemplate` nên cuộn cùng lưới; không dùng `AreRowDetailsFrozen` | Cần kiểm tra khi cuộn ở app thật |
| Cột Component hẹp (125px) | Cắt chữ + tooltip hiện mã chi tiết | Có thể nới cột Actions nếu cần |
| Chip Status trong 65px | Chữ ngắn "✔ done" / "✖ missing" | Có thể nới nhẹ cột STATUS |
| `FT600` không có `INotifyPropertyChanged` | `RefreshStepsGridCore()` dựng lại lưới, kèm dựng lại `Level2Items` | Selection bị reset mỗi lần dựng lại → `FocusGridRowAction` chọn lại step đang cân |
| Mờ dòng / bỏ chọn khi click lại | Code-behind thuần hiển thị (`ShotWeightWindow.xaml.cs`) | Đúng quy ước: code-behind chỉ làm việc không bind được |

## 5. Hành vi tương tác

```mermaid
stateDiagram-v2
    [*] --> Normal
    Normal --> Selected : click một dòng step
    Selected --> Normal : click lại chính dòng đó
    Selected --> Selected : click dòng step khác (chuyển highlight)
    Selected --> Selected : click nút Scale/Reset/Delete hoặc vùng chi tiết (giữ nguyên)
    Selected --> Selected : Save/Reset → lưới dựng lại, step đang cân được chọn lại
    note right of Selected
        Dòng chọn: viền nổi + xổ chi tiết
        Dòng khác: opacity 0.35
    end note
```

## 6. Luồng dữ liệu

```mermaid
flowchart LR
    BOM["IBomService<br/>Level 2 (cache)"] --> VM
    REPO["IScaleStepRepository<br/>FT600 đã lưu"] --> VM
    SESSION["_scaleDataFinal<br/>dòng đang cân"] --> VM
    VM["ShotWeightViewModel<br/>RefreshLevel2Details()"] -->|Level2Items, Level2Formula,<br/>Level2HasMissing| FT600["FT600 (NotMapped)"]
    FT600 --> TPL["Level2DetailsTemplate<br/>(RowDetailsTemplate)"]
    TPL --> GRID["dgTotalSteps"]
    GRID -->|SelectionChanged / click| CB["Code-behind<br/>mờ dòng, bỏ chọn"]
```

## 7. File đã thay đổi

| File | Nội dung |
|------|----------|
| [`models/Level2DetailItem.cs`](../../models/Level2DetailItem.cs) | DTO một dòng chi tiết |
| [`models/ef/entities/FT600.partial.cs`](../../models/ef/entities/FT600.partial.cs) | `Level2Items`, `Level2Formula`, `Level2HasMissing` (`[NotMapped]`) |
| [`UI/WPF/ViewModels/ShotWeightViewModel.cs`](../../UI/WPF/ViewModels/ShotWeightViewModel.cs) | `RefreshLevel2Details()` |
| [`UI/WPF/Styles/ShotWeightStyles.xaml`](../../UI/WPF/Styles/ShotWeightStyles.xaml) | `StepRowStyle`, `Level2DetailsTemplate`, `NullToCollapsed` |
| [`UI/WPF/Converters/ScaleConverters.cs`](../../UI/WPF/Converters/ScaleConverters.cs) | `NullToCollapsedConverter` |
| [`UI/WPF/ShotWeightWindow.xaml`](../../UI/WPF/ShotWeightWindow.xaml) | thuộc tính `dgTotalSteps` (RowDetails, RowStyle, sự kiện chuột) |
| [`UI/WPF/ShotWeightWindow.xaml.cs`](../../UI/WPF/ShotWeightWindow.xaml.cs) | mờ dòng, click lại để bỏ chọn |

## 8. Checklist review trên app thật

- [ ] Cột chi tiết (Component → Deduct) thẳng hàng với cột lưới chính, kể cả sau khi kéo đổi độ rộng cột.
- [ ] Cuộn ngang lưới (nhiều cột): phần chi tiết cuộn cùng lưới, không lệch, các cột bên phải PART để trống.
- [ ] Chọn step → viền nổi, chi tiết xổ ra, dòng khác mờ; click lại → về bình thường.
- [ ] Nút Scale/Reset/Delete ở dòng mờ vẫn bấm được.
- [ ] Sau Save/Reset, step đang cân được chọn lại và trạng thái chi tiết (✔/✖, công thức) cập nhật.
- [ ] Step không có chi tiết con (Logo, Studs đầu chuỗi) hiện "No components".
- [ ] Màu viền dòng được chọn, tên Component dài, chip "✖ missing" trong cột 65px có chấp nhận được.
- [ ] Cột STATUS (đã căn lại 2026-11-04) thẳng hàng với MACHINE / Step No.
