# ShotWeightWindow – Technical Documentation

**Project:** SSSW – Shot Weight Scale System  
**Module:** `ShotWeightWindow.xaml` / `ShotWeightViewModel.cs`  
**Version:** 1.0 · WPF Refactor  
**Date:** 2026-05-17  
**Author:** Cong Nguyen · cong.nguyen@framas.com  
**Confidential:** framas Internal Use Only

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [MVVM Architecture](#2-mvvm-architecture)
3. [Business Logic](#3-business-logic)
4. [Calculation Formulas](#4-calculation-formulas)
5. [Confirm & Database Flow](#5-confirm--database-flow)
6. [UI Layout](#6-ui-layout)
7. [FT600 Field Reference](#7-ft600-field-reference)
8. [User Guide](#8-user-guide)
9. [Common Errors](#9-common-errors)
10. [Converters & Styles](#10-converters--styles)
11. [Stored Procedures](#11-stored-procedures)
12. [Related Database Tables](#12-related-database-tables)

---

## 1. System Overview

**ShotWeightWindow** is the main screen of the SSSW application (Shot-weight Scale Station) — a WPF interface running on production floor workstations.

### What it does

- Weighs injection molding shots step-by-step according to the production BOM.
- Auto-reads data from the **digital scale**, **QR/Barcode scanner**, and **employee RFID cards**.
- Calculates **Part Weight (C021)**, **Runner Weight (C022)**, and **Total Part Weight (C023)** for each step.
- Saves results to `FT600` (database) and updates Hydra status in `FT601`.
- Compares actual values against standard (STD) within tolerance thresholds: **±1%** (OK) / **±3%** (Warn).

### Technical Stack

| Item | Detail |
|------|--------|
| Main Namespace | `SSSW.UI.WPF` |
| ViewModel | `SSSW.UI.WPF.ViewModels.ShotWeightViewModel` |
| Default Size | 1920 × 1080 px (Full HD), `WindowStyle=None` |
| UI Framework | WPF (.NET) + DevExpress WPF Controls |
| Hardware SDK | ScanAndScale.Core — BarcodeDriver, RfidDriver, ScaleDriver |
| ORM / DB | Entity Framework Core — `DbContextDogeWH` (IDbContextFactory) |
| DI Framework | Microsoft.Extensions.DependencyInjection |
| Auto-update | AutoUpdaterDotNET |

---

## 2. MVVM Architecture

### 2.1 Layer Breakdown

| Layer | File / Class | Responsibility |
|-------|-------------|----------------|
| View | `ShotWeightWindow.xaml` | Full XAML UI — binds to ViewModel, NO business logic |
| Code-behind | `ShotWeightWindow.xaml.cs` | Win32 drag, driver init, driver→VM event bridge, DevExpress bridge |
| ViewModel | `ShotWeightViewModel.cs` | All business logic, Commands, ObservableCollections, weight calculations |
| Model – Domain | `FT600`, `FT601` (EF entities) | Entities storing weighing results and Hydra step master data |
| Model – UI | `ReferenceRow`, `ScaleModels.cs` | Reference Values table model, enum `ToleranceCategory` |
| Hardware Drivers | `BarcodeDriver`, `RfidDriver`, `ScaleDriver` | Singleton drivers from ScanAndScale.Core |
| Data Layer | `DbContextDogeWH` (EF Core) | SQL Server: FT600, FT601, FT029, FT606, FT608 |

### 2.2 Overall Data Flow

```
App Start → DI Container → ShotWeightWindow → Window.Loaded → InitializeAsync
  └─ LoadDataAsync (FT601) → LookUpEdit (StepCodeMaster)
  └─ Select Step / Scan Barcode → GetDataAsync → _scaleDataFinal (BOM)
  └─ Scale hardware → ScaleDriver event → Dispatcher.Invoke → ScaleValue (UI)
  └─ Press Save → ExecuteSave (calc C021–C024) → RefreshUI + Reference Panel
  └─ Press Confirm → INSERT FT600 → UPDATE FT601.C017=true → ResetNewLoop
```

### 2.3 Hardware Drivers

> ⚠ **Thread Safety:** All hardware events fire from a background thread. Code-behind **must** use `Dispatcher.Invoke` / `InvokeAsync` before updating UI or ViewModel properties.

| Driver | Protocol | Initialization | Event |
|--------|----------|---------------|-------|
| BarcodeDriver | Zebra CoreScanner SDK | Singleton: `BarcodeDriver.Instance` | DataValueChanged |
| RfidDriver | Serial Port (COM) | Singleton: `RfidDriver.Instance` | DataValueChanged |
| ScaleDriver | TCP/IP (IP:Port) | New instance (dispose/re-init per config) | DataValueChanged |

### 2.4 Dependency Injection

`ShotWeightViewModel` is resolved from the DI container with:
- `IDbContextFactory<DbContextDogeWH>` — creates a DbContext per-operation (thread-safe)
- `IServiceProvider` — resolves auxiliary forms (`frmMainView`, `frmUpdateMasterData`)
- `ILogger<ShotWeightViewModel>` — error logging (Microsoft.Extensions.Logging)

Commands use `RelayCommand` (sync) and `AsyncRelayCommand` (async) following pure MVVM.

### 2.5 View Callbacks (Action\<T\>)

Because some View operations cannot be bound (DevExpress, WPF focus), the ViewModel exposes `Action` delegates:

| Action | Description |
|--------|-------------|
| `ClearBarcodeAction` | Clear the barcode input field |
| `ClearRfidAction` | Clear the RFID input field |
| `FocusRfidNameAction` | Focus the Employee Name field |
| `ClearStepComboAction` | Reset DevExpress LookUpEdit to null |
| `SetStepComboAction` | Set the LookUpEdit value |
| `FocusGridRowAction` | Scroll and select a row in dgTotalSteps |
| `ApplyHardwareConfigAction` | Initialize the 3 drivers from DB config |

---

## 3. Business Logic

### 3.1 Startup (InitializeAsync)

1. Initialize AutoUpdater (DownloadPath, events).
2. Query DB: `sp_MaterialGetCompanyName` → `_mesocomp`; `sp_MaterialGetMesoyear` → `_mesoYear`.
3. Map mesocomp → `EnumLocation` → WindowTitle (`fVN / fKV / fFT / fIN / fGE – Shotweight Station`).
4. Read `FT608_Config` by `Environment.MachineName` → deserialize `ConfigModel`. If not found → INSERT defaults.
5. Call `ApplyHardwareConfigAction` → code-behind initializes BarcodeDriver, RfidDriver, ScaleDriver.
6. Set `UsagePct = ConfigSystem.PercentOfUserNonWoven`, `ReadOnly = ConfigSystem.EnableReadScale`.
7. Call `LoadDataAsync()`.

### 3.2 LoadDataAsync — Load Master Data

Uses a `CancellationTokenSource` linked with a timeout (default 30s). Cancels any previous request.

1. Show overlay + enable Cancel button.
2. `Task.Run`: query `FT601s` WHERE `C021=true AND Mesoyear=_mesoYear`.
3. Project → `List<StepSelectModel>` → `StepCodeMaster` (ItemsSource for LookUpEdit).
4. Dispatch to UI thread → update `StepCodeMaster`. Hide overlay.

### 3.3 GetDataAsync — Build Step List (New Session)

1. `ResetNewLoop()` — clear previous state.
2. Query `FT601` by `C007` (FGItemCode) → `_stepItemCodeScale`.
3. Call `sp_getBomWinlineOfItemFG(@itemFG)` → `_allStepsFG`.
4. For each BOM step: find the matching FT601 in `_dataHydra`, create a temporary `FT600` (`AllowScale` by rule: `REX/Z-VHXXXXXX=true`, unmatched code=false).
5. Handle multi-size molds: find `sameMolds` by MoldId+Machine+prefix → add to `_scaleData`.
6. Sort `_scaleDataFinal` by C015 (sequence) → C027. Pre-fill values for Stud/Logo/Cleat_Ring/REX from DB.
7. Check previous steps: if `C021 == 0` → warn "The previous step has not been weighed."

### 3.4 RFID — Employee Authentication

1. Driver reads card → `OnRfidValueChanged(rfidCode)`.
2. Query `FT029_Operator_RFID` WHERE `C000.Contains(rfidCode)`.
3. Check `DepartmentInfor`: must belong to IT or QC (FT031). If not → reject, clear RFID.
4. If valid: `RfidName = C001`, `UserName = "Code · Name"`.
5. If not in DB: Focus `tbRFIDName` → type name + Enter → `OnRfidNameEnterAsync` → INSERT into FT029.

### 3.5 Barcode — Find Step from QR

1. Driver reads → `OnBarcodeScannedAsync(barcode)`.
2. Query `FT606_Label` WHERE `c001 = barcode` → `_labelInfo`.
3. Find `_stepSelected` in `_dataHydra` by `_labelInfo.c000` (FT601.Id).
4. `FilterStepCombo` → set LookUpEdit → `TriggerStepSelectionAsync`.

---

## 4. Calculation Formulas

When **Save** is pressed, `ExecuteSave` branches by material type:

### 4.1 Injection Items — 1st Weighing (C024 == 0)

```
C024 = ScaleValue
```
*Total weight after ejection (part + runner). Unit: grams.*

### 4.2 Injection Items — 2nd Weighing (C024 > 0)

Remove the runner, keep only the part on the scale:

```
C023 = ScaleValue
C022 (Runner=YES) = Round( (C024 − C023 × prsShot) / prsShot, 3 )
C022 (Runner=NO)  = 0
C021 = C023 − Σ C023_prev − Σ C023_non-inj
```

Where:
- `prsShot` = `_articlePaisShotFinaly` (C028)
- `Σ C023_prev` = sum of C023 for all steps with `C015 = current_C015 − 1`
- `Σ C023_non-inj` = sum of C023 for REX / Z-VHXXXXXX steps at the same C015

For Studs, Logo, Cleat_Ring:
```
C036 = Round( ScaleValue / prsShot, 2 )
```

### 4.3 Non-Injection (REX) — Receptacle (catCheck == null)

```
C024 = C023 = C021 = ScaleValue
C036 = Round( ScaleValue / C025, 2 )        // C025 = quantity
```

### 4.4 Non-Injection (REX) — Nonwoven / Mesh (catCheck != null)

```
usage   = Round( (ScaleValue × _percentOfUsage / 100) / C028, 3 )
unusage = (ScaleValue − usage × C028) / C028

C024        = ScaleValue
C023 = C021 = usage
C022        = unusage
C036        = Round( usage / 2, 2 )         // per foot
```

### 4.5 Multi-size Mold

When `sameMolds.Count > 1` (multiple sizes sharing one mold):

```
sumPW    = Σ( C023_i × C028_i )
pairShot = Σ C028_i

C024[each size] = _rowSelected.C024          // shared
C022[each size] = (C024 − sumPW) / pairShot  // if C023 > 0, else 0
```

### 4.6 Cascade Update (recalculate subsequent steps)

After Save, subsequent steps with `C024 > 0` are recalculated:

```
Same sequence:   C021 = C023 − _rowSelected.C023
                 C022 = C024 − C021
Later sequence:  C021 = C023 − prev.C023
                 C022 = C024 − C021
```

### 4.7 Tolerance System

```
Δ      = Actual − STD
Δ%     = (Actual − STD) / STD × 100
```

| Level | Condition | Background | Text | Border |
|-------|-----------|-----------|------|--------|
| **Idle** | STD or Actual not available | `#F5F5F5` | `#546E7A` | `#CFD8DC` |
| **OK** | \|Δ%\| ≤ 1% | `#D4F7DC` | `#1B5E20` | `#4CAF50` |
| **WARN** | 1% < \|Δ%\| ≤ 3% | `#FFF3CD` | `#E65100` | `#FF9800` |
| **ERR** | \|Δ%\| > 3% | `#FDE8E8` | `#B71C1C` | `#F44336` |

ScaleCard border/background = **worst** ToleranceCategory among 4 Reference rows.

---

## 5. Confirm & Database Flow

`ExecuteConfirmAsync` steps:

1. Check `_operatorInfo.Id != Guid.Empty` — RFID must have been scanned and validated.
2. Check multi-size molds (`sameMolds`): if > 0, skip completeness check.
3. For each row with `AllowScale=true`: `C023 == 0 AND C024 == 0` → warn "Scale not completed for step: XXX".
4. Filter: `insert = _scaleDataFinal WHERE AllowScale=true AND C021 > 0`.
5. Set metadata: `C010=EmployeeCode`, `C011=EmployeeName`, `CreatedDate=now`, `CreatedMachine`, `Mesocomp`, `Mesoyear`.
6. Transaction: `db.FT600s.AddRangeAsync(insert)`.
7. `FT601s.ExecuteUpdateAsync`: SET `C017=true, ModifiedDate, ModifiedMachine` WHERE conditions match.
8. `SaveChangesAsync + CommitAsync`. On error → `RollbackAsync`.
9. Reset: clear `_labelInfo`, call `ClearStepComboAction`, `ResetNewLoop()`.

---

## 6. UI Layout

Window: 1920 × 1080 px, `WindowStyle=None` (no OS chrome). 3-row Grid:

| Row | Height | Content |
|-----|--------|---------|
| 0 | 44px | Custom Title Bar (background `#0D1117`) |
| 1 | * | Main content — Left Panel (1\*) + Right Area (4\*) |
| 2 | 72px | Bottom Bar — Save / Cancel / Confirm buttons |

### 6.1 Title Bar

| Component | Binding / Action |
|-----------|-----------------|
| "framas" logo | Static text, Bold 16pt white |
| WindowTitle | `vm.WindowTitle` — e.g. "fVN – Shotweight Station" |
| UserName | `vm.UserName` — "EmployeeCode · EmployeeName" after RFID scan |
| Reload | `ReloadCommand` → `LoadDataAsync(timeout=30s)` |
| History | `HistoryViewCommand` → opens `frmMainView` |
| Hydra | `HydraCommand` → sync FT601 from Hydra ERP |
| Settings | `SettingsCommand` → opens `frmUpdateMasterData` |
| Update | `UpdateCommand` → AutoUpdaterDotNET |
| Window drag | Win32 `SendMessage(WM_SYSCOMMAND, SC_DRAGMOVE)` |

### 6.2 Left Panel — STEP INFORMATION

Key bindings:

| Control | Binding | Mode | Notes |
|---------|---------|------|-------|
| Name | StepName | OneWay | TextWrapping, ReadOnly |
| Code | StepCode | OneWay | ReadOnly |
| Machine | Machine | OneWay | ReadOnly |
| Quantity | Qty | OneWay | ReadOnly |
| Partitioning | ActualPairs | OneWay | Editable when Adj.=ON; Enter → recalculate |
| Adj. CheckBox | AllowPartitionAdj | TwoWay | Unlocks Partitioning & Usage % |
| Runner | RunnerText | TwoWay | YES / NO |
| Usage % | UsagePct | OneWay | Editable for REX partial; Enter → recalculate |
| Remark | Remark | TwoWay | Syncs to `_scaleDataFinal[*].C038` on change |
| Select Step | StepCodeMaster | — | DevExpress LookUpEdit with Excel-style column filters |

### 6.3 Bottom Bar

| Button | Color | Command | Function |
|--------|-------|---------|----------|
| ↓ Save | `#1A237E` | SaveCommand | Calc C021/C022/C023/C024, store in memory |
| Cancel | `#E65100` | CancelCommand | Cancel session, reset all state |
| Confirm | `#2E7D32` | ConfirmCommand | Persist to DB, update FT601.C017=true |

---

## 7. FT600 Field Reference

| Field | Type | Description |
|-------|------|-------------|
| id | Guid | Primary key |
| C002 | string | Step Item Code |
| C003 | string | Step Item Name |
| C004 | string | Machine code |
| C008 | string | Size |
| C010 | string | Employee Code (operator) |
| C011 | string | Employee Name |
| C012 | string | Barcode / QR code |
| C013 | string | FG Item Code |
| C014 | string | FG Item Name |
| C015 | int? | Sequence index |
| C017 | decimal? | Article Pairs per Shot |
| C018 | decimal? | Mold Pairs per Shot |
| **C021** | double? | **PART WEIGHT (g/prs)** |
| **C022** | double? | **RUNNER WEIGHT (g/prs)** |
| **C023** | double? | **TOTAL PART WEIGHT (g/prs)** |
| **C024** | double? | **TOTAL WEIGHT INJECTION (g)** |
| C025 | double? | Quantity |
| C026 | string | Main Item Code |
| C027 | string | Main Item Name |
| C028 | double? | Actual Pairs per Shot |
| C029 | Guid? | Label Id (FT606) |
| C032 | Guid? | FT601 Id |
| C033 | string | Category Code (REX logic) |
| C035 | double? | Usage % (nonwoven/mesh) |
| C036 | double? | Weight per piece (Studs, Logo, Cleat_Ring, REX) |
| C038 | string | Remark |
| AllowScale | bool | Whether step can be weighed |
| CreateDate | DateTime | DB save timestamp |
| Mesocomp | string | Company code (VNT1, FKV, FTT1...) |
| Mesoyear | int | Meso production year |

---

## 8. User Guide

### 8.1 Startup and Login

1. Launch SSSW — wait for the "Loading…" overlay to disappear.
2. Check device status dots: 🟢 green = connected, 🔴 red = disconnected.
3. Scan RFID card (or type Employee ID → Enter). Employee name appears in the title bar.

> **Note:** Only IT / QC staff are authorized. First-time users: type name → Enter → confirm "Yes".

### 8.2 Select a Production Step

**Method 1 – Scan QR/Barcode:** Click the "Barcode / QR Scan" field → scan → step auto-selected.  
**Method 2 – Manual:** Click "Select Step" → popup → filter/click desired row.

After selection: left panel fills automatically, Total Steps grid shows BOM, History updates.

### 8.3 Weighing Workflow

#### Injection — 1st Weighing (C024)
1. Place entire shot (part + runner) on scale. Wait for "Stable Value" badge → green.
2. Click **[Scale]** in the grid OR press **Save** in the bottom bar.
3. "TOTAL WEIGHT INJECTION" column updates.

#### Injection — 2nd Weighing (C023)
1. Remove the runner, keep only the part. Wait for stability → press **Save**.
2. C023, C022, C021 are calculated. Reference Panel colors by tolerance.

#### Non-Injection (REX) — Single Weighing
1. Place material on scale → wait for stability → press **Save** (once only).
2. System auto-calculates using receptacle or nonwoven formula.

### 8.4 Adjustments

| Adjustment | How | Notes |
|------------|-----|-------|
| Partitioning (prs) | Tick Adj. → type number → Enter | When actual pairs differ from Hydra |
| Usage % | Tick Adj. → type % → Enter | Only for REX nonwoven/mesh |
| Runner | Select YES / NO in ComboBox | NO → C022 = 0 |
| Remark | Type in Remark field | Auto-syncs C038 on all rows |
| Switch step | Click [Scale] on another row | Checks previous step was weighed |
| Reset row | Click [Reset] on target row | C021/C022/C023/C024 = 0 |

### 8.5 Manual Entry (scale disconnected)

1. Click the large scale number field (Consolas 44pt).
2. Type a numeric value (e.g., `36.5`) — only digits and decimal point accepted.
3. Press Enter → processed as hardware input.

> A yellow warning tooltip appears for invalid characters (auto-closes after 2 sec).

### 8.6 Confirm and Save

1. Ensure valid RFID has been scanned.
2. Ensure all `AllowScale=true` steps are weighed.
3. Click **Confirm** → saves to DB → success message → screen resets.

### 8.7 Hydra Sync

1. Click ⚡ **Hydra** button in the title bar.
2. Calls `sp_GetFullStepItemHydraIsRun` → INSERTs new FT601 records.
3. Auto-calls `LoadDataAsync` to refresh the LookUpEdit.

---

## 9. Common Errors

| Message | Cause | Resolution |
|---------|-------|-----------|
| "The previous step has not been weighed" | Previous step C021 = 0 | Weigh the previous step first |
| "Label does not match the item being weighed" | QR code mismatch | Scan correct label or select correct step |
| "Do not allow to scale this step" | AllowScale = false | Skip or contact IT |
| "Scale not completed for step: XXX" | AllowScale=true step not weighed | Complete all required steps |
| "RFID card not yet scanned" | No RFID before Confirm | Scan employee RFID card |
| "Employee does not have permission" | Not IT/QC department | Use an IT/QC card |
| "Employee ID not found" | Card code not in FT029 | Type name + Enter to register |
| "Load data failure" | DB connection error | Check network and SQL Server |
| "Transaction error" | Error during Confirm DB save | Check InnerException, verify DB permissions |

---

## 10. Converters & Styles

### 10.1 Converters (`ScaleConverters.cs`)

| Converter | Input | Output |
|-----------|-------|--------|
| `ToleranceToBrushConverter` | ToleranceCategory | Brush – cell background |
| `ToleranceToBorderBrushConverter` | ToleranceCategory | Brush – cell border |
| `ToleranceToForeConverter` | ToleranceCategory | Brush – cell text |
| `BooleanToVisibilityConverter` | bool | Visible / Collapsed |
| `DriverStatusToColorConverter` | DriverStatus | Brush – device dot |
| `StableToForegroundConverter` | bool (IsStable) | Green=stable, Orange=fluctuating |
| `TareToBackgroundConverter` | bool (IsTare) | Scale area background |
| `InverseBoolToVisibilityConverter` | bool | Inverse (False→Visible) |

### 10.2 Key Styles (`ShotWeightStyles.xaml`)

| Key | TargetType | Description |
|-----|-----------|-------------|
| `LabelStyle` | TextBlock | Segoe UI 20pt, `#78909C` |
| `FieldStyle` | TextBox | Segoe UI 20pt, background `#FAFAFA`, border `#CFD8DC` |
| `EditableFieldStyle` | TextBox | Inherits FieldStyle, `IsReadOnly=false`, white background |
| `CardStyle` | Border | White background, border `#CFD8DC`, CornerRadius=6 |
| `BottomBtn` | Button | Bold 15pt, rounded, hover/press/disabled opacity |
| `TitleIconBtn` | Button | Segoe MDL2 Assets, 32×32, hover `#3A4A7A` |
| `GridStyle` | DataGrid | AutoGenerateColumns=False, alternating rows, Segoe UI 12pt |
| `PrimaryButton` | Button | `#1976D2` — Scale button |
| `WarningButton` | Button | `#F9A825` — Reset button |
| `DangerButton` | Button | `#D32F2F` — Delete button |

---

## 11. Stored Procedures

| Stored Procedure | Parameters | Returns |
|-----------------|-----------|---------|
| `sp_MaterialGetCompanyName` | (none) | string — company code |
| `sp_MaterialGetMesoyear` | (none) | int — current Meso year |
| `sp_getBomWinlineOfItemFG` | `@itemFG = FGItemCode` | `List<BomWinlineModel>` — BOM for FG item |
| `sp_GetFullStepItemHydraIsRun` | (none) | `List<HydraItemDetailModel>` — all running Hydra steps |
| `sp_GetCategorryOfItem` | `@ItemCode` (comma-separated) | `List<CategoryOfItemModel>` — categories |

---

## 12. Related Database Tables

| Table / Entity | Purpose | Main Operations |
|---------------|---------|----------------|
| `FT600` | Shot weight weighing results | INSERT (Confirm), SELECT (History) |
| `FT601` | Hydra running step master data | SELECT (LoadData), UPDATE C017=true (Confirm), INSERT (Hydra sync) |
| `FT029_Operator_RFID` | Employee and RFID card registry | SELECT (auth), INSERT (new registration) |
| `FT031` | Departments (IT, QC) | SELECT (permission check) |
| `FT606_Label` | QR code label information | SELECT (lookup by barcode) |
| `FT608_Config` | System configuration per machine | SELECT (load config), INSERT (create new) |

---

*SSSW – Shot Weight Station Documentation · framas Internal Use Only · v1.0 – 17/05/2026 · Cong Nguyen · cong.nguyen@framas.com*
