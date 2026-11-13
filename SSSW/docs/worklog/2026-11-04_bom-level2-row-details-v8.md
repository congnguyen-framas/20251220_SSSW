# 2026-11-04 — Cài đặt UI BOM Level 2 Row Details (phương án A, v8)

## Yêu cầu
Triển khai code cho `docs/brainstorm/BOM_LEVEL2_UI_OPTIONS.md` (phương án A, thiết kế v8), sau đó cập nhật doc đầy đủ.

## Đã làm
- `SSSW/UI/WPF/Styles/ShotWeightStyles.xaml`: viết lại `Level2DetailsTemplate` theo v8 — thứ tự cột Component / Status / Step / Qty (PCS) / Unit bám cột lưới 0–4 (Actions, STATUS, MACHINE, MAIN / MAIN CODE, Step No.); Weighed (g/prs) / g per piece / Deduct (g) là `Grid` 3 phần đều trong bề rộng cột 5 (STEP / STEP CODE). Bảng chỉ rộng đúng 6 cột đầu, chỉ có đường kẻ ngang (`#ECEFF1`) giữa các dòng, tiêu đề nền `#EAF1F8`. Cập nhật comment mô tả ánh xạ cột.
- `SSSW/UI/WPF/ShotWeightWindow.xaml`: cột Actions 125 → 140px; `StackPanel` → `WrapPanel` để 3 nút Scale/Reset/Delete nằm 1 hàng, kéo hẹp thì tự rớt dòng.
- Doc: cập nhật `docs/brainstorm/BOM_LEVEL2_UI_OPTIONS.md`, `docs/plan/BOM_LEVEL2_ROW_DETAILS_UI.md` (trạng thái, §4, thêm §9 ghi chú cài đặt), thêm worklog này + dòng trong `INDEX.md`.
- Các phần v3 đã có sẵn giữ nguyên: `Level2DetailItem`, `FT600.Level2*`, `RefreshLevel2Details()`, `StepRowStyle` (viền 4 cạnh), mờ dòng / click lại bỏ chọn trong code-behind.

## Quyết định / giả định
- Yêu cầu 12 (dòng chọn hiện Active): giữ logic `UpdateStepStatuses()` — Active là step đang cân (`_rowSelected`), click dòng chỉ xem chi tiết, không đổi trạng thái. Ghi rõ ở plan §9.
- Viền chọn: chọn cách (1) — viền `DataGridRow` (cạnh phải ở cuối lưới khi cuộn ngang).
- Indent của vài dòng trong template chưa đồng nhất (không ảnh hưởng chạy).

## Việc còn mở
- Review trên app thật theo checklist §8 của plan (căn cột, cuộn ngang, cột 65px của Status chip).
- Quyết định có cần click-để-Active và khung viền theo viewport (cách 2) không.

## Build/test
- `dotnet build` không chạy được ở máy này: lỗi NU1101 thiếu package `ScanAndScale.Driver` (nhiễu môi trường, không phải lỗi code). Hai file XAML đã parse XML hợp lệ. Chưa chạy thử giao diện.
