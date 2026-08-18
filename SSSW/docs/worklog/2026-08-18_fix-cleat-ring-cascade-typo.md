# 2026-08-18 — Fix cascade `ExecuteSave` ghi đè sai Part Weight của Cleat_Ring khi cân REX (Cleat/đinh sắt) cùng Step_No

## Yêu cầu

> với lần cân có đinh sắt, Cleat và Cleat_Ring, thì khi cân đinh sắt xong nó
> chuyển sang bước tiếp theo thì nó lại thay đổi PartWeight của Cleat_Ring,
> nhưng 2 bước này chung Step_No, thì nó chạy riêng lẻ, ko có trừ ra, tìm
> nguyên nhân và khắc phục

Kèm ảnh chụp `TOTAL STEPS`: `Cleat_Ring` (Step No. 1) có `PART (g/prs)` =
**-38.51** (âm, rõ ràng sai) ngay sau khi Save xong dòng REX `Cleat AD-1977 ST
Silver Met 080A` (cũng Step No. 1).

## Nguyên nhân

`ExecuteSave()` → khối cascade "cập nhật `C021`/`C022` cho các bước sau"
([ShotWeightViewModel.cs:1377-1385](../../UI/WPF/ViewModels/ShotWeightViewModel.cs#L1377-L1385))
lọc `toUpdate` bằng điều kiện `!x.C003.StartsWith("Ring")` — nhưng field
`C003` của dòng Cleat_Ring có giá trị bắt đầu bằng **`"Cleat_Ring"`**, không
phải `"Ring"`, nên `StartsWith("Ring")` luôn `false` → điều kiện loại trừ
không bao giờ đúng → dòng Cleat_Ring **không** bị loại khỏi `toUpdate` như ý
đồ ban đầu (xem các chỗ khác trong cùng file đều check đúng
`StartsWith("Cleat_Ring")`, ví dụ dòng 1250, 1295, 1078, 1009 — đây là lỗi gõ
thiếu tiền tố `Cleat_` chỉ ở đúng chỗ này).

Hệ quả: khi Save xong dòng REX `Cleat` (Step No. 1, `_rowSelected` trỏ vào
đây), cascade chạy qua Cleat_Ring (cũng Step No. 1, khác `C002`, không phải
REX, không phải Stud/Inlay) và rơi vào nhánh
`item.C015 == _rowSelected.C015`:

```
item.C021 = item.C023 - _rowSelected.C023;   // 4.49 - 43.00 = -38.51 ← khớp chính xác số trong ảnh
```

Tức là code coi Cleat_Ring như 1 bước "kế thừa" phải trừ đi phần weight của
REX Cleat cùng Step_No — trong khi thực tế 2 item này **độc lập hoàn toàn**,
chỉ tình cờ trùng Step_No, không có quan hệ cộng dồn/trừ ra.

## Đã làm

- [SSSW/UI/WPF/ViewModels/ShotWeightViewModel.cs:1381](../../UI/WPF/ViewModels/ShotWeightViewModel.cs#L1381)
  — sửa `!x.C003.StartsWith("Ring")` → `!x.C003.StartsWith("Cleat_Ring")` để
  loại đúng dòng Cleat_Ring khỏi cascade, khớp với convention dùng
  `"Cleat_Ring"` ở mọi chỗ khác trong file.

## Quyết định / giả định

- Chỉ sửa đúng typo này (điểm khác biệt duy nhất so với convention chung của
  file), không mở rộng thêm điều kiện loại trừ khác (`Logo`,
  `Outer_Stud`/`Inner_Stud` — có ở 1 số chỗ pre-fill khác nhưng không có
  trong cascade block gốc) vì không có dữ liệu/triệu chứng cụ thể cho thấy
  chúng cũng bị ảnh hưởng tương tự — tránh sửa vượt phạm vi bug report.

## Việc còn mở

- Chưa build/test được (xem Build/test) — cần verify lại bằng cách cân đúng
  kịch bản trong ảnh (REX Cleat + Cleat_Ring cùng Step_No) trên máy có DB
  thật, xác nhận `PART (g/prs)` của Cleat_Ring không còn bị REX Cleat ghi đè.

## Build/test

`dotnet build` vẫn fail restore do thiếu quyền truy cập private NuGet feed
(`NU1101: ScanAndScale.Driver`) — môi trường sandbox, không liên quan tới
thay đổi. Đã verify bằng tay: `4.49 - 43.00 = -38.51` khớp chính xác giá trị
lỗi trong ảnh chụp màn hình user cung cấp, xác nhận đúng root cause trước khi
sửa.
