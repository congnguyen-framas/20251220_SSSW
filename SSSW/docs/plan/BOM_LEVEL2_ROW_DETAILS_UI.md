# Thiết kế UI: BOM Level 2 Row Details trong TOTAL STEPS

- **Ngày**: 2026-11-04 · **Nhánh**: `feature/use-bom-level-2`
- **Trạng thái**: ⏳ **Chờ review** hình v5 (giữ 8 cột như v3, chỉ 5 cột đầu căn theo lưới chính). Code hiện có trên nhánh còn theo v3 (cả 8 cột bám cột lưới) — nếu duyệt v5 thì phải sửa `Level2DetailsTemplate`; chưa code thêm gì theo yêu cầu này
- **Liên quan**: [`brainstorm/BOM_LEVEL2_UI_OPTIONS.md`](../brainstorm/BOM_LEVEL2_UI_OPTIONS.md) (các phương án), [`tech/BUSINESS_LOGIC.md`](../tech/BUSINESS_LOGIC.md) §4.8 (công thức `C021`), session [`2026-11-04`](../sessions/2026-11-04_status-column-align-bom-l2-ui-options.md)

## 1. Yêu cầu

1. Chọn phương án A (mở rộng dòng), thiết kế lại: chọn step nào thì **highlight** và **làm mờ** các dòng còn lại; **bỏ** tiêu đề "BOM LEVEL 2…"; click dòng lần nữa thì về bình thường.
2. Bỏ dòng *self* trong bảng chi tiết.
3. Chuyển toàn bộ cột và mô tả sang tiếng Anh.
4. Sắp lại thứ tự cột chi tiết (số xanh) và căn cùng đường dọc với cột lưới chính (số đỏ).
5. **Giữ 8 cột như v3, nhưng chỉ cần trùng vị trí với các cột đầu của lưới; các cột sau không cần căn.**

## 2. Hình thiết kế (v5 — theo đúng lưới thật, chỉ 5 cột đầu căn thẳng)

![Option A v5](../image/bom_level2_option_A_v5_first_cols_aligned.png)

- Lưới TOTAL STEPS thật có **nhiều cột** (hơn 20, kéo ngang bằng thanh cuộn): Actions, STATUS, MACHINE, MAIN / MAIN CODE, Step No., STEP / STEP CODE, SIZE, TOTAL WEIGHT, TOTAL PART, PART, RUNNER / EXCESS MAT., Actual Partitioning, PPIC Partitioning, Mold Pairs Shot, Quantity, … Hình v4 vẽ đủ các cột này (đường kẻ ngang cắt ở Quantity giống màn hình thật).
- Đường gióng đỏ nét đứt = ranh giới cột lưới chính. **Chỉ 5 cột chi tiết đầu (Component, Status, Step, Qty, Unit) nằm trong ranh giới đó.** 3 cột sau (Weighed, g / piece, Deduct) có độ rộng cố định riêng, đặt liền sau cột Unit, không bám cột nào của lưới.
- Dải chi tiết rộng bằng cả lưới và **cuộn ngang cùng lưới**; mọi vùng bên phải 3 cột tự do để trống.
- Dữ liệu trong hình là minh họa (bố cục lấy từ ảnh chụp lưới thật, FG `6112042502`; Qty/trọng lượng chi tiết do mình đặt ví dụ).

## 3. Ánh xạ cột chi tiết → cột lưới chính

**Chỉ số cột thật của `dgTotalSteps`**: 0 Actions · 1 STATUS · 2 MACHINE · 3 MAIN / MAIN CODE · 4 Step No. · 5 STEP / STEP CODE · 6 SIZE · 7 TOTAL WEIGHT · 8 TOTAL PART · 9 PART · 10 RUNNER / EXCESS MAT. · 11 Actual Partitioning · 12 PPIC Partitioning · 13 Mold Pairs Shot · 14 Quantity · 15 Weight per Unit · 16 Unit · 17 FG / FG CODE · … (còn các cột MACHINE GROUP, MOLD ID, REMARKS, ARTICLE, HYDRA ORDER, CATEGORY, Usage %…).

| # | Cột chi tiết | Căn theo cột lưới chính (index trong `dgTotalSteps`) | Rộng (px) | Nguồn dữ liệu |
|---|--------------|------------------------------------------------------|-----------|---------------|
| 1 | Component | Actions (0) | 125 | `ItemStepName` của BOM L2 |
| 2 | Status | STATUS (1) | 65 | đã cân / chưa cân |
| 3 | Step | MACHINE (2) | 75 | `C015` của chi tiết con, `REX` nếu non-injection |
| — | *(trống)* | MAIN / MAIN CODE (3) | 150 | — |
| 4 | Qty (PCS) | Step No. (4) | 80 | `Quantity` BOM L2 |
| 5 | Unit | STEP / STEP CODE (5) | 300 | `Unit` BOM L2 |
| 6 | Weighed (g/prs) | **không căn** — liền sau Unit | 110 (cố định) | `C021` đã cân |
| 7 | g / piece | **không căn** | 100 (cố định) | `UnitWeightPerPiece` |
| 8 | Deduct (g) | **không căn** | 100 (cố định) | `Qty × g/piece` |

Quy tắc căn: chỉ cột chi tiết 1–5 phải thẳng hàng với cột lưới 0, 1, 2, 4, 5. Cột 6–8 không phụ thuộc SIZE / TOTAL WEIGHT / TOTAL PART / PART nên không bị ảnh hưởng khi kéo đổi hay thêm bớt các cột phía sau của lưới.

> Lưu ý: lưới thật đã có sẵn các cột **Quantity (14)**, **Weight per Unit (15)**, **Unit (16)** nằm xa bên phải. Phương án này không dùng chúng cho chi tiết con vì chúng thuộc về chính step đang cân; chi tiết con dùng Qty/Unit riêng của BOM Level 2 đặt ở các cột nhìn thấy ngay (Step No., STEP / STEP CODE).

## 4. Tính khả thi

**Khả thi.** Lý do và điểm cần lưu ý (code hiện có là bản v3 — cả 8 cột bám cột lưới; v5 chỉ cần đổi 3 ô cuối sang chiều rộng cố định):

| Vấn đề | Giải pháp | Ghi chú |
|--------|-----------|---------|
| 5 cột chi tiết đầu phải thẳng hàng với cột lưới | Mỗi ô là `Border` có `Width` bind `Columns[i].ActualWidth` của `DataGrid` (`RelativeSource AncestorType=DataGrid`) cho cột lưới 0–5 | Kéo đổi độ rộng cột vẫn thẳng hàng. Không dùng `ColumnDefinition` vì `RelativeSource` không hoạt động ở đó |
| 3 cột chi tiết cuối không cần căn | `Border` với `Width` cố định (110 / 100 / 100) đặt liền sau cột Unit | Không phụ thuộc các cột sau của lưới → ít rủi ro hơn v3 |
| Lưới thật có nhiều cột hơn các mockup trước (v1–v3 đã lược bớt) | v4/v5 vẽ đủ bộ cột thật; cột MAIN / MAIN CODE để trống | Nếu đổi thứ tự 6 cột đầu của lưới thì phải sửa chỉ số trong `Level2DetailsTemplate` |
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

- [ ] 5 cột chi tiết đầu (Component → Unit) thẳng hàng với Actions / STATUS / MACHINE / Step No. / STEP; 3 cột sau (Weighed, g / piece, Deduct) đọc rõ, không bị cắt.
- [ ] Cuộn ngang lưới (nhiều cột): phần chi tiết cuộn cùng lưới, không lệch.
- [ ] Chọn step → viền nổi, chi tiết xổ ra, dòng khác mờ; click lại → về bình thường.
- [ ] Nút Scale/Reset/Delete ở dòng mờ vẫn bấm được.
- [ ] Sau Save/Reset, step đang cân được chọn lại và trạng thái chi tiết (✔/✖, công thức) cập nhật.
- [ ] Step không có chi tiết con (Logo, Studs đầu chuỗi) hiện "No components".
- [ ] Màu viền dòng được chọn, tên Component dài, chip "✖ missing" trong cột 65px có chấp nhận được.
- [ ] Cột STATUS (đã căn lại 2026-11-04) thẳng hàng với MACHINE / Step No.
