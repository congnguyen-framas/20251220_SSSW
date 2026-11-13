# 📝 Session: User Manual (Operator + Developer, EN/VI) với ảnh chú thích

- **Ngày**: 2026-11-13 · **Nhánh**: `feature/use-bom-level-2`

## 1. Yêu cầu
Viết user manual chi tiết các thao tác cân cho từng trường hợp; dữ liệu giả lập để chụp màn hình (không vào được DB nhà máy); ký hiệu lên ảnh rồi dẫn vào manual; lưu trong `docs/` cho **Operator** và **Dev**, tiếng **Anh** và **Việt**. Yêu cầu bổ sung: **không sửa code ứng dụng**, chỉ làm manual.

## 2. Thực hiện
- `docs/manual/OPERATOR_MANUAL.{vi,en}.md` (thay `OPERATOR_MANUAL.md` cũ): 12 mục, 10 trường hợp cân (A injection 2 lần, B REX, C non-woven/mesh, D BOM level 2, E chain, F partitioning N, G runner NO, H nhiều size, I PU L/R + popup, J Cleat_Ring + step khóa), màu dung sai, nút dòng, Remark/Confirm/Cancel, bảng thông báo, sự cố.
- `docs/manual/DEVELOPER_MANUAL.{vi,en}.md`: build/release, kiến trúc, bản đồ màn hình→code, cột FT600, state machine, ma trận trường hợp→code, công thức, Confirm transaction, phần cứng/thread, harness chụp ảnh, checklist mở rộng.
- `docs/manual/images/` (36 ảnh PNG 1920×1200 hoặc crop, có khung đỏ + số).
- `docs/manual/screenshot-harness/*.cs.txt`: mã nguồn harness. **Đuôi `.cs.txt` có chủ đích** — `SSSW.csproj` compile mọi `*.cs` dưới `SSSW/`.

## 3. Cách chụp ảnh (không đụng source app)
Harness (project WPF tạm, đặt ngoài repo) tham chiếu DLL đã build, dựng `Main` + `ShotWeightWindow` + `ShotWeightViewModel` **thật** với service giả (`IDialogService`, `IBomService`, `IScaleStepRepository`, `IDbContextFactory = null`), seed `_scaleDataFinal` / `_level2ByStep` bằng reflection, rồi gọi `OnGridScale`, `OnScaleValueChanged`, `SaveCommand`… → mọi con số trong ảnh do code thật tính. Chi tiết: DEVELOPER_MANUAL §12.

## 4. Giả định / lưu ý
- Dữ liệu trong ảnh (mã hàng, máy, tên, trọng lượng, lịch sử) là **giả lập**; lịch sử/STANDARD mô phỏng `LoadRefHistoryAsync`, đăng nhập RFID và hộp thoại là giả lập (hộp thoại vẽ lại từ nội dung thật).
- Dung sai trong code so theo **gram** (`DeltaLevel1/2` = 1 g / 3 g), trong khi `CLAUDE.md` ghi ±1 % / ±3 % — manual theo code; nên xác nhận với PO.
- Tab Step/FG đang `Visibility="Hidden"` nên không có manual cho màn FG. Hộp thoại WinForms (`frmRfidInput`, History, Settings) và popup `LookUpEdit` chỉ mô tả bằng chữ.
- Popup cân (PU / step khóa) mô tả theo code, chưa chạy thử trên app thật (session 2026-11-13 cũng ghi nhận).
- Entity `FT600` vẫn `FT600_dev` — nhắc trong DEVELOPER_MANUAL §2.4.

## 5. Kết quả
Harness chạy hết 16 scene không lỗi; app `dotnet build SSSW/SSSW.csproj` (Debug) thành công. Không thay đổi file nào trong source ứng dụng.

## 6. Việc còn mở
- Nhờ operator/PO đọc lại các thuật ngữ & ngưỡng dung sai.
- Khi UI/công thức đổi: chạy lại harness, cập nhật ảnh + cả 2 ngôn ngữ (Docs-Sync).

## 7. Bổ sung (2026-11-14)
- Thêm `manual/OPERATOR_MANUAL.id.md` (tiếng Indonesia, dịch từ bản EN, dùng chung ảnh `images/`); cập nhật liên kết chéo ở bản EN/VI và `docs/INDEX.md`.
