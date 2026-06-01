// ============================================================
//  gen_shotweight_doc.js
//  Chạy: node gen_shotweight_doc.js
//  Yêu cầu: npm install -g docx  (hoặc npm install docx trong thư mục này)
//  Output:  ShotWeightWindow_Documentation.docx
// ============================================================
// (Đây là bản rút gọn – file đầy đủ nằm trong outputs folder)
// Để chạy: mở terminal trong thư mục này, chạy lệnh:
//   npm install docx
//   node gen_shotweight_doc.js
// ============================================================
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, HeadingLevel, AlignmentType, BorderStyle, WidthType,
  ShadingType, VerticalAlign, PageNumber, PageBreak, LevelFormat,
  TableOfContents
} = require('docx');
const fs = require('fs');

const borderSingle = { style: BorderStyle.SINGLE, size: 1, color: "CFD8DC" };
const cellBorders = { top: borderSingle, bottom: borderSingle, left: borderSingle, right: borderSingle };
const cellPad = { top: 100, bottom: 100, left: 150, right: 150 };

function h1(text) {
  return new Paragraph({ heading: HeadingLevel.HEADING_1, spacing: { before: 360, after: 160 }, children: [new TextRun({ text, bold: true, size: 36, font: "Arial", color: "1A237E" })] });
}
function h2(text) {
  return new Paragraph({ heading: HeadingLevel.HEADING_2, spacing: { before: 280, after: 120 }, children: [new TextRun({ text, bold: true, size: 28, font: "Arial", color: "1565C0" })] });
}
function h3(text) {
  return new Paragraph({ heading: HeadingLevel.HEADING_3, spacing: { before: 200, after: 80 }, children: [new TextRun({ text, bold: true, size: 24, font: "Arial", color: "37474F" })] });
}
function body(text) {
  return new Paragraph({ spacing: { after: 120 }, children: [new TextRun({ text, font: "Arial", size: 22 })] });
}
function bullet(text, level = 0) {
  return new Paragraph({ numbering: { reference: "bullets", level }, spacing: { after: 80 }, children: [new TextRun({ text, font: "Arial", size: 22 })] });
}
function numbered(text) {
  return new Paragraph({ numbering: { reference: "numbers", level: 0 }, spacing: { after: 80 }, children: [new TextRun({ text, font: "Arial", size: 22 })] });
}
function sp() { return new Paragraph({ spacing: { after: 120 }, children: [new TextRun("")] }); }

function formulaBox(label, formula, desc = "") {
  return new Table({
    width: { size: 13000, type: WidthType.DXA }, columnWidths: [3000, 10000],
    rows: [new TableRow({ children: [
      new TableCell({ borders: cellBorders, width: { size: 3000, type: WidthType.DXA }, shading: { fill: "1565C0", type: ShadingType.CLEAR }, margins: cellPad, verticalAlign: VerticalAlign.CENTER,
        children: [new Paragraph({ children: [new TextRun({ text: label, bold: true, font: "Arial", size: 22, color: "FFFFFF" })] })] }),
      new TableCell({ borders: cellBorders, width: { size: 10000, type: WidthType.DXA }, shading: { fill: "E8F0FE", type: ShadingType.CLEAR }, margins: cellPad,
        children: [
          new Paragraph({ children: [new TextRun({ text: formula, font: "Consolas", size: 22, bold: true, color: "1A237E" })] }),
          ...(desc ? [new Paragraph({ spacing: { before: 60 }, children: [new TextRun({ text: desc, font: "Arial", size: 20, color: "546E7A" })] })] : [])
        ] })
    ]})]
  });
}

function tbl(cols, rows) {
  const w = Math.floor(13000 / cols.length);
  return new Table({
    width: { size: 13000, type: WidthType.DXA }, columnWidths: cols.map(() => w),
    rows: [
      new TableRow({ tableHeader: true, children: cols.map(c => new TableCell({ borders: cellBorders, width: { size: w, type: WidthType.DXA }, shading: { fill: "1565C0", type: ShadingType.CLEAR }, margins: cellPad, children: [new Paragraph({ children: [new TextRun({ text: c, bold: true, font: "Arial", size: 22, color: "FFFFFF" })] })] })) }),
      ...rows.map((row, ri) => new TableRow({ children: row.map(cell => new TableCell({ borders: cellBorders, width: { size: w, type: WidthType.DXA }, shading: { fill: ri % 2 === 0 ? "F5F7FA" : "FFFFFF", type: ShadingType.CLEAR }, margins: cellPad, children: [new Paragraph({ children: [new TextRun({ text: cell, font: "Arial", size: 22 })] })] })) }))
    ]
  });
}

const children = [
  // COVER
  new Paragraph({ alignment: AlignmentType.CENTER, spacing: { before: 2400, after: 400 }, children: [new TextRun({ text: "ShotWeightWindow.xaml", bold: true, font: "Arial", size: 56, color: "1A237E" })] }),
  new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 200 }, children: [new TextRun({ text: "Tài liệu kỹ thuật – Shot Weight Station", font: "Arial", size: 32, color: "546E7A" })] }),
  new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 200 }, children: [new TextRun({ text: "Kiến trúc  ·  Logic nghiệp vụ  ·  Công thức tính toán  ·  Hướng dẫn UI", font: "Arial", size: 24, color: "78909C" })] }),
  new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 3200 }, children: [new TextRun({ text: "Phiên bản: 1.0  |  17/05/2026  |  SSSW · framas Internal", font: "Arial", size: 20, color: "90A4AE" })] }),
  new Paragraph({ children: [new PageBreak()] }),

  // TOC
  new TableOfContents("Mục lục", { hyperlink: true, headingStyleRange: "1-3" }),
  new Paragraph({ children: [new PageBreak()] }),

  // 1. TỔNG QUAN
  h1("1. Tổng quan hệ thống"),
  body("ShotWeightWindow là màn hình chính của ứng dụng SSSW (Shot-weight Scale Station), giao diện WPF tại xưởng sản xuất. Nhân viên QC/IT sử dụng để cân trọng lượng shot injection molding, đọc dữ liệu từ cân điện tử/barcode/RFID, tính toán Part Weight/Runner Weight/Total Weight, lưu kết quả DB và so sánh với STD theo dung sai ±1%/±3%."),
  sp(),
  tbl(["Thông tin", "Chi tiết"], [
    ["Namespace", "SSSW.UI.WPF"], ["ViewModel", "ShotWeightViewModel"], ["Kích thước", "1920×1080px, WindowStyle=None"],
    ["Hardware SDK", "ScanAndScale.Core (Barcode/Rfid/Scale Driver)"], ["ORM/DB", "EF Core – DbContextDogeWH"], ["DI", "Microsoft.Extensions.DependencyInjection"],
  ]),
  new Paragraph({ children: [new PageBreak()] }),

  // 2. KIẾN TRÚC
  h1("2. Kiến trúc – Mô hình MVVM"),
  tbl(["Tầng", "File / Class", "Vai trò"], [
    ["View", "ShotWeightWindow.xaml", "XAML binding – không có business logic"],
    ["Code-behind", "ShotWeightWindow.xaml.cs", "Win32 drag, driver init, event bridge, DevExpress bridge"],
    ["ViewModel", "ShotWeightViewModel.cs", "Toàn bộ business logic, Commands, tính toán trọng lượng"],
    ["Model – Domain", "FT600, FT601", "EF entities – kết quả cân và master step"],
    ["Model – UI", "ReferenceRow, ScaleModels.cs", "Reference Values, ToleranceCategory enum"],
    ["Hardware", "BarcodeDriver / RfidDriver / ScaleDriver", "ScanAndScale.Core singleton/instance drivers"],
    ["Data Layer", "DbContextDogeWH", "SQL Server – FT600/601/029/606/608"],
  ]),
  sp(),
  h2("2.1 Luồng dữ liệu"),
  numbered("App Start → DI Container → ShotWeightWindow → Window.Loaded → InitializeAsync"),
  numbered("LoadDataAsync → FT601s (active, mesoyear) → StepCodeMaster (LookUpEdit)"),
  numbered("Chọn Step / Quét Barcode → GetDataAsync → _scaleDataFinal (BOM + multi-size)"),
  numbered("Hardware Scale → ScaleDriver event → Dispatcher.Invoke → ScaleValue (UI)"),
  numbered("Nhấn Save → ExecuteSave → tính C021/C022/C023/C024 → RefreshUI + Reference Panel"),
  numbered("Nhấn Confirm → Transaction INSERT FT600 + UPDATE FT601.C017=true → ResetNewLoop"),
  sp(),
  h2("2.2 View Callbacks (Action<T>)"),
  body("ViewModel expose Actions để code-behind gán (những thao tác không thể bind):"),
  bullet("ClearBarcodeAction / ClearRfidAction – xóa text ô hardware"),
  bullet("FocusRfidNameAction – focus ô Employee Name"),
  bullet("ClearStepComboAction / SetStepComboAction – control DevExpress LookUpEdit"),
  bullet("FocusGridRowAction – scroll + select dòng trong DataGrid"),
  bullet("ApplyHardwareConfigAction – khởi tạo 3 drivers sau load config DB"),
  new Paragraph({ children: [new PageBreak()] }),

  // 3. GIAO DIỆN
  h1("3. Cấu trúc giao diện UI"),
  body("Cửa sổ chia 3 hàng: Title Bar (44px) / Main Content (*) / Bottom Bar (72px). Overlay Loading span toàn bộ."),
  sp(),
  h2("3.1 Title Bar (nền #0D1117)"),
  tbl(["Thành phần", "Binding / Hành động"], [
    ["WindowTitle", "vm.WindowTitle – vd: fVN – Shotweight Station"],
    ["UserName", "vm.UserName – EmployeeCode · Name (sau RFID)"],
    ["Reload/History/Hydra/Settings/Update", "ReloadCommand / HistoryViewCommand / HydraCommand / SettingsCommand / UpdateCommand"],
    ["Minimize/Maximize/Close", "Click handlers code-behind; Kéo = Win32 SendMessage(WM_SYSCOMMAND, SC_DRAGMOVE)"],
  ]),
  sp(),
  h2("3.2 Panel trái – STEP INFORMATION"),
  tbl(["Control", "Binding", "Mode", "Ghi chú"], [
    ["Name", "StepName", "OneWay", "ReadOnly, TextWrapping"],
    ["Partitioning (prs)", "ActualPairs", "OneWay", "IsReadOnly ← IsActualPairsReadOnly; Enter → OnActualPairsEnter"],
    ["Adj. CheckBox", "AllowPartitionAdj", "TwoWay", "Mở khóa Partitioning + Usage %"],
    ["Runner", "RunnerText", "TwoWay", "YES / NO"],
    ["Usage %", "UsagePct", "OneWay", "IsReadOnly ← IsUsagePctReadOnly; Enter → OnUsagePctEnter"],
    ["Remark", "Remark", "TwoWay, PropertyChanged", "Sync sang _scaleDataFinal[*].C038"],
    ["Select Step (LookUpEdit)", "StepCodeMaster", "ItemsSource", "Event bridge qua cbStepName_EditValueChangedAsync"],
  ]),
  sp(),
  h2("3.3 TOTAL STEPS DataGrid (410px)"),
  tbl(["Cột", "Binding", "Ý nghĩa"], [
    ["Actions", "GridScaleCommand / GridResetCommand", "Nút Scale (xanh), Reset (vàng), Delete (đỏ)"],
    ["STATUS", "StatusDotColor + StatusText", "Pending / Active / Done"],
    ["TOTAL WEIGHT INJECTION (g)", "C024 (F2)", "Tổng trọng lượng sau ejection (lần cân 1)"],
    ["TOTAL PART WEIGHT (g/prs)", "C023 (F2)", "Tổng part weight tích lũy (lần cân 2)"],
    ["PART WEIGHT (g/prs)", "C021 (F2)", "Part weight bước này"],
    ["RUNNER WEIGHT (g/prs)", "C022 (F2)", "Runner/excess material"],
  ]),
  sp(),
  h2("3.4 RFID & Barcode"),
  body("Hai card nằm cạnh nhau. RfidStatus / BarcodeStatus binding DriverStatusToColor (xanh/đỏ/xám). Gõ thủ công + Enter kích hoạt handler tương ứng."),
  sp(),
  h2("3.5 Scale Value + Reference Values"),
  body("Số cân lớn (Consolas 44pt, chỉ nhận số, Enter = submit). ScaleCardBorderBrush / ScaleCardBackground đổi màu theo worst tolerance. dgRefValues 4 dòng: Total W_Injection / Total PW / Part Weight / Runner Weight – so sánh STD vs Actual vs Δ."),
  new Paragraph({ children: [new PageBreak()] }),

  // 4. LOGIC
  h1("4. Logic nghiệp vụ"),
  h2("4.1 GetDataAsync – Build _scaleDataFinal"),
  numbered("ResetNewLoop() → query FT601 (C007=FGItemCode) → _stepItemCodeScale"),
  numbered("sp_getBomWinlineOfItemFG → _allStepsFG"),
  numbered("Mỗi BOM step: tìm FT601 trong _dataHydra → tạo FT600 tạm (AllowScale theo loại)"),
  numbered("Multi-size mold: tìm sameMolds theo MoldId+Machine+prefix → thêm vào _scaleData"),
  numbered("Sắp xếp theo C015. Pre-fill Stud/Logo/REX từ bản ghi DB gần nhất"),
  numbered("Kiểm tra bước trước: C021 == 0 → cảnh báo"),
  sp(),
  h2("4.2 RFID xác thực"),
  numbered("OnRfidValueChanged → query FT029 WHERE C000.Contains(rfidCode)"),
  numbered("Kiểm tra DepartmentInfor thuộc IT/QC (FT031). Nếu không → từ chối"),
  numbered("Nếu chưa có DB → tbRFIDName Enter → OnRfidNameEnterAsync → INSERT FT029"),
  sp(),
  h2("4.3 Barcode quét QR"),
  numbered("OnBarcodeScannedAsync → query FT606_Label WHERE c001=barcode → _labelInfo"),
  numbered("Tìm _stepSelected trong _dataHydra → FilterStepCombo → SetStepComboAction → TriggerStepSelectionAsync"),
  new Paragraph({ children: [new PageBreak()] }),

  // 5. CÔNG THỨC
  h1("5. Công thức tính toán (ExecuteSave)"),
  h2("5.1 Injection – Lần cân 1 (C024 == 0)"),
  formulaBox("C024", "C024 = ScaleValue", "Tổng trọng lượng sau ejection (gram). Lần cân đầu tiên."),
  sp(),
  h2("5.2 Injection – Lần cân 2 (C024 > 0)"),
  formulaBox("C023", "C023 = ScaleValue", "Total Part Weight (g/prs) – đọc trực tiếp từ cân."),
  formulaBox("C022 (Runner=YES)", "C022 = Round( (C024 - C023 × prsShot) / prsShot, 3 )", "Runner weight (g/prs). prsShot = _articlePaisShotFinaly. Nếu Runner=NO → C022 = 0."),
  formulaBox("C021", "C021 = C023 - ΣC023(bước trước) - ΣC023(non-inj cùng bước)", "Part weight bước này. Trừ đi tích lũy từ bước trước và vật liệu phụ (REX/Z-VHXXXXXX) cùng sequence."),
  formulaBox("C036 (Studs/Logo/Cleat_Ring)", "C036 = Round( ScaleValue / prsShot, 2 )", "Trọng lượng/piece (g). Với bước khác: C036 = 0."),
  sp(),
  h2("5.3 Non-injection REX – Receptacle (catCheck == null)"),
  formulaBox("C024 = C023 = C021", "= ScaleValue", "100% vật liệu là part weight."),
  formulaBox("C036", "C036 = Round( ScaleValue / C025, 2 )", "g/piece, C025 = quantity."),
  sp(),
  h2("5.4 Non-injection REX – Nonwoven/Mesh (catCheck != null)"),
  formulaBox("usage", "usage = Round( (ScaleValue × _percentOfUsage / 100) / C028, 3 )", "Phần sử dụng thực tế (g/prs). C028 = prsShot."),
  formulaBox("unusage", "unusage = (ScaleValue - usage × C028) / C028", "Phần waste (g/prs)."),
  formulaBox("C024", "C024 = ScaleValue", ""),
  formulaBox("C023 = C021", "= usage", ""),
  formulaBox("C022", "C022 = unusage", ""),
  formulaBox("C036", "C036 = Round( usage / 2, 2 )", "g per foot (1 đôi = 2 chân)."),
  sp(),
  h2("5.5 Khuôn nhiều size"),
  formulaBox("sumPW", "sumPW = Σ(C023ᵢ × C028ᵢ)", "Tổng part weight × pairs tất cả size."),
  formulaBox("C024 (mỗi size)", "C024 = _rowSelected.C024", "Dùng chung giá trị cân tổng."),
  formulaBox("C022 (mỗi size)", "C022 = (C024 - sumPW) / Σ C028ᵢ  (nếu C023 > 0, else 0)", "Runner phân bổ theo tổng pairs."),
  sp(),
  h2("5.6 Tolerance – Reference Panel"),
  formulaBox("Δ%", "Δ% = (Actual - STD) / STD × 100", "STD = C024/C023/C021/C022 lần cân gần nhất trong DB."),
  sp(),
  tbl(["Mức", "Điều kiện", "Màu nền", "Màu viền Scale Card"], [
    ["Idle", "Chưa có STD hoặc Actual", "#F5F5F5", "#CFD8DC"],
    ["OK", "|Δ%| ≤ DeltaLevel1 (~1%)", "#D4F7DC (xanh nhạt)", "#4CAF50"],
    ["WARN", "DeltaLevel1 < |Δ%| ≤ DeltaLevel2 (~3%)", "#FFF3CD (vàng nhạt)", "#FF9800"],
    ["ERR", "|Δ%| > DeltaLevel2", "#FDE8E8 (đỏ nhạt)", "#F44336"],
  ]),
  new Paragraph({ children: [new PageBreak()] }),

  // 6. CONFIRM
  h1("6. Luồng Confirm – Lưu DB"),
  numbered("Kiểm tra _operatorInfo.Id != Guid.Empty (đã quét RFID)"),
  numbered("Kiểm tra tất cả step AllowScale=true: C023==0 AND C024==0 (REX) → báo lỗi"),
  numbered("Lọc insert = _scaleDataFinal WHERE AllowScale=true AND C021 > 0"),
  numbered("Gán metadata: C010/C011=Employee, CreatedDate, CreatedMachine, Mesocomp, Mesoyear"),
  numbered("Transaction: AddRange(insert) → FT600s"),
  numbered("ExecuteUpdateAsync FT601s: SET C017=true, ModifiedDate WHERE C004/C007 match AND C017=false"),
  numbered("SaveChangesAsync + CommitAsync → nếu lỗi RollbackAsync"),
  numbered("Reset: ClearStepComboAction + ResetNewLoop"),
  new Paragraph({ children: [new PageBreak()] }),

  // 7. DATA MODEL
  h1("7. Data Model – FT600 (fields chính)"),
  tbl(["Field", "Kiểu", "Ý nghĩa"], [
    ["id", "Guid", "Khóa chính"],
    ["C002", "string", "Step Item Code"],
    ["C003", "string", "Step Item Name"],
    ["C004", "string", "Machine"],
    ["C015", "int?", "Sequence Index (thứ tự bước)"],
    ["C021", "double?", "PART WEIGHT (g/prs) – kết quả chính"],
    ["C022", "double?", "RUNNER WEIGHT (g/prs)"],
    ["C023", "double?", "TOTAL PART WEIGHT (g/prs)"],
    ["C024", "double?", "TOTAL WEIGHT INJECTION (g)"],
    ["C028", "double?", "Pairs per Shot thực tế"],
    ["C033", "string", "Category Code (REX logic)"],
    ["C035", "double?", "Usage %"],
    ["C036", "double?", "Trọng lượng/piece"],
    ["C038", "string", "Remark"],
    ["AllowScale", "bool", "Cho phép cân step này"],
    ["CreateDate", "DateTime", "Thời gian lưu DB"],
  ]),
  new Paragraph({ children: [new PageBreak()] }),

  // 8. HƯỚNG DẪN
  h1("8. Hướng dẫn sử dụng UI"),
  h2("8.1 Khởi động & Đăng nhập"),
  numbered("Khởi động SSSW → chờ overlay Loading biến mất"),
  numbered("Kiểm tra chấm thiết bị: xanh = connected, đỏ = mất kết nối"),
  numbered("Quét thẻ RFID (hoặc gõ Employee ID → Enter). Tên hiện ra ở Employee Name và title bar"),
  body("Chỉ nhân viên IT / QC được phép cân. Lần đầu: gõ tên → Enter → xác nhận đăng ký."),
  sp(),
  h2("8.2 Chọn Step"),
  body("Cách 1 – Quét QR/Barcode: hệ thống tự tìm và chọn step."),
  body("Cách 2 – LookUpEdit: Click 'Select Step' → gõ từ khóa → click dòng cần chọn."),
  sp(),
  h2("8.3 Cân Injection (2 lần)"),
  numbered("Lần 1: Đặt toàn bộ shot (part+runner) → Stable badge xanh → nhấn Save → C024 ghi nhận"),
  numbered("Lần 2: Lấy runner ra → chỉ giữ part → Stable → nhấn Save → C023/C022/C021 tính toán"),
  sp(),
  h2("8.4 Cân REX – Non-injection (1 lần)"),
  numbered("Đặt vật liệu → Stable → nhấn Save → hệ thống tự tính theo receptacle hoặc nonwoven logic"),
  sp(),
  h2("8.5 Điều chỉnh"),
  body("Partitioning: Tick Adj. → gõ số → Enter. Usage %: Tick Adj. → gõ % → Enter (chỉ REX nonwoven). Runner: chọn YES/NO. Remark: gõ trực tiếp."),
  sp(),
  h2("8.6 Confirm"),
  numbered("Đảm bảo đã quét RFID + tất cả step đã cân"),
  numbered("Click Confirm (xanh lá) → lưu DB → reset màn hình"),
  sp(),
  h2("8.7 Nhập thủ công (manual mode)"),
  body("Click vào ô số cân lớn → gõ số → Enter. Chỉ nhận số và dấu thập phân. Tooltip cảnh báo 2 giây nếu ký tự không hợp lệ."),
  new Paragraph({ children: [new PageBreak()] }),

  // 9. LỖI
  h1("9. Lỗi và cảnh báo thường gặp"),
  tbl(["Thông báo", "Nguyên nhân", "Xử lý"], [
    ["The previous step has not been weighed", "Bước trước C015-1 chưa có C021>0", "Cân bước trước trước"],
    ["Label does not match the item being weighed", "QR code không khớp step", "Quét lại label đúng"],
    ["Do not allow to scale this step", "AllowScale = false", "Bỏ qua hoặc liên hệ IT"],
    ["Scale not completed for step: XXX", "Step AllowScale=true chưa cân", "Cân đủ tất cả step"],
    ["RFID card not yet scanned", "Chưa quét RFID trước Confirm", "Quét thẻ RFID"],
    ["Employee does not have permission", "Không thuộc IT/QC", "Dùng thẻ IT/QC"],
    ["Employee ID not found", "Mã thẻ chưa có trong FT029", "Gõ tên + Enter để đăng ký"],
    ["Load data failure", "Lỗi kết nối DB", "Kiểm tra mạng, SQL Server"],
    ["Transaction error", "Lỗi khi Confirm lưu DB", "Xem InnerException, kiểm tra quyền DB"],
  ]),
  sp(),

  // 10. STORED PROC
  h1("10. Stored Procedures & Database Tables"),
  h2("10.1 Stored Procedures"),
  tbl(["Stored Procedure", "Tham số", "Kết quả"], [
    ["sp_MaterialGetCompanyName", "(none)", "string – mã company"],
    ["sp_MaterialGetMesoyear", "(none)", "int – năm Meso hiện tại"],
    ["sp_getBomWinlineOfItemFG", "@itemFG = FGItemCode", "List<BomWinlineModel> – BOM winline"],
    ["sp_GetFullStepItemHydraIsRun", "(none)", "List<HydraItemDetailModel> – step Hydra đang chạy"],
    ["sp_GetCategorryOfItem", "@ItemCode = codes", "List<CategoryOfItemModel>"],
  ]),
  sp(),
  h2("10.2 Database Tables"),
  tbl(["Bảng / Entity", "Mục đích", "Thao tác"], [
    ["FT600", "Kết quả cân shot weight", "INSERT (Confirm), SELECT (History)"],
    ["FT601", "Master step Hydra", "SELECT, UPDATE C017=true, INSERT (Hydra sync)"],
    ["FT029_Operator_RFID", "Nhân viên và thẻ RFID", "SELECT, INSERT"],
    ["FT031 (Department)", "Phòng ban IT/QC", "SELECT (kiểm tra quyền)"],
    ["FT606_Label", "Label QR code", "SELECT"],
    ["FT608_Config", "Cấu hình theo máy", "SELECT, INSERT"],
  ]),
];

const doc = new Document({
  numbering: {
    config: [
      { reference: "bullets", levels: [{ level: 0, format: LevelFormat.BULLET, text: "•", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
      { reference: "numbers", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] }
    ]
  },
  styles: {
    default: { document: { run: { font: "Arial", size: 22 } } },
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true, run: { size: 36, bold: true, font: "Arial", color: "1A237E" }, paragraph: { spacing: { before: 400, after: 200 }, outlineLevel: 0 } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true, run: { size: 28, bold: true, font: "Arial", color: "1565C0" }, paragraph: { spacing: { before: 280, after: 140 }, outlineLevel: 1 } },
      { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true, run: { size: 24, bold: true, font: "Arial", color: "37474F" }, paragraph: { spacing: { before: 200, after: 100 }, outlineLevel: 2 } }
    ]
  },
  sections: [{
    properties: {
      page: { size: { width: 15840, height: 12240 }, margin: { top: 1080, right: 1080, bottom: 1080, left: 1080 } }
    },
    headers: {
      default: new Header({ children: [new Paragraph({ border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: "1565C0", space: 4 } }, children: [new TextRun({ text: "SSSW – ShotWeightWindow  |  Tài liệu kỹ thuật  |  v1.0  |  17/05/2026", font: "Arial", size: 18, color: "546E7A" })] })] })
    },
    footers: {
      default: new Footer({ children: [new Paragraph({ border: { top: { style: BorderStyle.SINGLE, size: 4, color: "1565C0", space: 4 } }, children: [new TextRun({ text: "framas Internal  |  Trang ", font: "Arial", size: 18, color: "546E7A" }), new TextRun({ children: [PageNumber.CURRENT], font: "Arial", size: 18, color: "546E7A" }), new TextRun({ text: " / ", font: "Arial", size: 18, color: "546E7A" }), new TextRun({ children: [PageNumber.TOTAL_PAGES], font: "Arial", size: 18, color: "546E7A" })] })] })
    },
    children
  }]
});

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync("ShotWeightWindow_Documentation.docx", buffer);
  console.log("✅ Done: ShotWeightWindow_Documentation.docx");
}).catch(err => {
  console.error("❌ Error:", err.message);
  process.exit(1);
});
