# ShotWeightWindow – Technical Documentation

**Project:** SSSW – Shot Weight Scale System  
**Module:** `ShotWeightWindow.xaml` / `ShotWeightWindow.xaml.cs` / `ShotWeightViewModel.cs`  
**Version:** 1.1 · WPF MVVM  
**Date:** 2026-06-01  
**Author:** Cong Nguyen · cong.nguyen@framas.com  
**Confidential:** framas Internal Use Only

> **⚠ Update note (2026-08-13):** `ShotWeightWindow` is **no longer the app's
> top-level `Window`**. A new shell window, `Main.xaml` (`SSSW.UI.WPF.Main` /
> `MainViewModel`), is now the real top-level `Window` launched from
> `Program.cs`. `ShotWeightWindow` and `ShotWeightFGWindow` were converted from
> `Window` to `UserControl` so `Main`'s body (`ContentControl`) can host either
> one — `Main`'s header has two tab buttons ("Step Component" / "Finished
> Goods") that swap `MainViewModel.ActiveContent` between cached instances of
> the two controls (default: Step, on startup). The old modal "Scan FG" dialog
> flow (`ScaneFGCommand` → `ShotWeightFGWindow.ShowDialog()`, with
> `SuspendDeviceEventsAction`/`ResumeDeviceEventsAction` to avoid double-
> handling hardware events) has been **removed** — `UserControl.Loaded`/
> `Unloaded`, which fire whenever `Main` swaps `ActiveContent`, now
> subscribe/unsubscribe each view's hardware events, giving the same
> "only the visible tab reacts to scans" guarantee without a dialog. The
> sections below (title bar, `Window`-specific attributes, drag/minimize/
> maximize/close in `ShotWeightWindow.xaml.cs`) describe the **pre-refactor**
> layout where `ShotWeightWindow` was still a `Window` — chrome (drag,
> Minimize/Maximize/Close, `WindowState`) now lives in `Main.xaml`/`Main.xaml.cs`
> instead. See `docs/worklog/2026-08-13_main-shell-step-fg-tabs.md` for the
> full change log.

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
| Code-behind | `ShotWeightWindow.xaml.cs` | Win32 drag, driver init, driver→VM event bridge, DevExpress bridge, input validation |
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

Because some View operations cannot be bound (DevExpress, WPF focus), the ViewModel exposes `Action` delegates that are wired in `OnLoaded`:

| Action | Description |
|--------|-------------|
| `ClearBarcodeAction` | Clear the barcode input field |
| `ClearRfidAction` | Clear the RFID input field |
| `FocusRfidNameAction` | Focus the `tbRFIDName` Employee Name field |
| `ClearStepComboAction` | Reset DevExpress LookUpEdit to null |
| `SetStepComboAction` | Set the LookUpEdit value by `StepItemCode` |
| `FocusGridRowAction` | Scroll and select a row in `dgTotalSteps` by step code |
| `ApplyHardwareConfigAction` | Initialize the 3 drivers from DB config |

### 2.6 Code-Behind Handler Summary

The code-behind (`ShotWeightWindow.xaml.cs`) contains **only** what cannot be done via binding:

| Handler | Trigger | What it does |
|---------|---------|-------------|
| `OnLoaded` | Window loaded | Registers driver events, wires Action callbacks, calls `InitializeAsync` |
| `OnClosing` | Window closing | Unregisters driver events, disposes all drivers |
| `ApplyHardwareConfig` | Called by VM | Maps `ConfigModel` → `BarcodeConfig / RfidConfig / ScaleConfig`, initializes drivers |
| `BarcodeDriver_DataValueChanged` | BarcodeDriver event | `InvokeAsync` → sets `BarcodeScannedValue`, calls `OnBarcodeScannedAsync` |
| `RfidDriver_DataValueChanged` | RfidDriver event | `Invoke` → sets `RfidCardCode`, calls `OnRfidValueChanged` |
| `ScaleDriver_DataValueChanged` | ScaleDriver event | `Invoke` → calls `OnScaleValueChanged(value, stable, tare, unit)` |
| `cbStepName_EditValueChangedAsync` | DevExpress LookUpEdit | Finds `StepSelectModel` → calls `OnStepSelectedAsync` |
| `tbActualPairs_KeyDown` | Enter key | Calls `OnActualPairsEnter(text)` |
| `tbUsagePct_KeyDown` | Enter key | Calls `OnUsagePctEnter(text)` |
| `tbRFIDName_KeyDown` | Enter key | Calls `OnRfidNameEnterAsync(text)` |
| `_txtBarcode_KeyDown` | Enter key in barcode textbox | Forces binding update, replays `BarcodeDriver_DataValueChanged` |
| `_txtRFID_KeyDown` | Enter key in RFID textbox | Forces binding update, replays `RfidDriver_DataValueChanged` |
| `_txtScale_KeyDown` | Enter key in scale textbox | Forces binding update, replays `ScaleDriver_DataValueChanged` with manual value |
| `TxtScale_PreviewTextInput` | Character typed in scale box | Blocks non-numeric chars, shows warning tooltip |
| `TxtScale_Pasting` | Paste into scale box | Blocks non-numeric paste, shows warning tooltip |
| `ShowScaleWarning` | Called by above two | Shows amber tooltip below scale box, auto-closes after 2 s |
| `_tbEmployee_MouseLeftButtonDown` | Click on employee name in title | Opens `frmRfidInput` dialog |
| `HistoryHeader_Click` | Click on history header | Calls `vm.ToggleHistory()` |
| `TitleBar_MouseLeftButtonDown` | Drag title bar | Win32 `SendMessage(WM_SYSCOMMAND, SC_DRAGMOVE)` |
| `btnMinimize_Click` | Minimize button | `WindowState = Minimized` |
| `btnMaximize_Click` | Maximize button | Toggles Normal ↔ Maximized |
| `btnClose_Click` | Close (red) button | `Close()` |
| `FocusStepInGrid` | Called by VM via Action | `ScrollIntoView` + `SelectedItem` on `dgTotalSteps` for matching `FT600.C002` |

---

## 3. Business Logic

### 3.1 Startup (InitializeAsync)

1. Initialize AutoUpdater (DownloadPath, events).
2. Query DB: `sp_MaterialGetCompanyName` → `_mesocomp`; `sp_MaterialGetMesoyear` → `_mesoYear`.
3. Map mesocomp → `EnumLocation` → `WindowTitle` (`fVN / fKV / fFT / fIN / fGE – Shotweight Station`).
4. Read `FT608_Config` by `Environment.MachineName` → deserialize `ConfigModel`. If not found → INSERT defaults.
5. Call `ApplyHardwareConfigAction` → code-behind initializes BarcodeDriver, RfidDriver, ScaleDriver.
6. Set `UsagePct = ConfigSystem.PercentOfUserNonWoven`, `ReadOnlyScale = !ConfigSystem.EnableReadScale`.
7. Call `LoadDataAsync()`.

### 3.2 LoadDataAsync — Load Master Data

Uses a `CancellationTokenSource` linked with a timeout (default 30 s). Cancels any previous request.

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
4. If valid: `RfidName = C001`, `UserName = "Code · Name"` (displayed in title bar with status dot).
5. If not in DB: Focus `tbRFIDName` → type name + Enter → `OnRfidNameEnterAsync` → INSERT into FT029.
6. Clicking the employee name in the title bar opens `frmRfidInput` dialog for manual RFID entry.

### 3.5 Barcode — Find Step from QR

1. Driver reads → `OnBarcodeScannedAsync(barcode)`.
2. Query `FT606_Label` WHERE `c001 = barcode` → `_labelInfo`.
3. Find `_stepSelected` in `_dataHydra` by `_labelInfo.c000` (FT601.Id).
4. `FilterStepCombo` → set LookUpEdit → `TriggerStepSelectionAsync`.
5. Manual entry: type in the Barcode field in the Left Panel and press Enter — replays the driver event.

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

Window: 1920 × 1080 px, `WindowStyle=None` (no OS chrome). 2-row Grid:

| Row | Height | Content |
|-----|--------|---------|
| 0 | 44 px | Custom Title Bar (background `#0D1117`) |
| 1 | * | Main content — Left Panel (1\*) + Right Area (4\*) |

### 6.1 Title Bar

6-column grid inside the dark title bar:

| Column | Component | Binding / Action |
|--------|-----------|-----------------|
| 1 | **"framas"** logo | Static, Bold 16 pt white |
| 2 | `WindowTitle` | `vm.WindowTitle` — e.g. "fVN – Shotweight Station" |
| 3 | Spacer | — |
| 4 | RFID status dot + **"RFID – Employee ID:"** label + employee name | Dot: `RfidStatus → DriverStatusToColor`; Text: `vm.UserName`; Click → opens `frmRfidInput` dialog |
| 5 | Icon buttons | Reload, History, Hydra, Settings, Update, Minimize, Maximize, Close (red `#C62828`) |

> The employee field (`_tbEmployee`) uses `IsEnabled="{Binding ReadonlyRfid}"` and `MouseLeftButtonDown` to open `frmRfidInput`.

### 6.2 Left Panel — STEP INFORMATION

Full-height card on the left (1\* wide). Contains a `ScrollViewer` with the following fields:

| Control | Binding | Mode | Notes |
|---------|---------|------|-------|
| Name | `StepName` | OneWay | `TextWrapping`, ReadOnly, min 2 lines |
| Code | `StepCode` | OneWay | ReadOnly |
| Machine | `Machine` | OneWay | ReadOnly |
| Quantity | `Qty` | OneWay | ReadOnly |
| Partitioning (prs) | `ActualPairs` | OneWay | Editable when `Adj.`=ON; Enter → recalculate |
| Adj. CheckBox | `AllowPartitionAdj` | TwoWay | Unlocks Partitioning & Usage % |
| Runner | `RunnerText` | TwoWay | ComboBox YES / NO |
| Usage % | `UsagePct` | OneWay | Editable for REX partial; Enter → recalculate |
| Size | `Size` | OneWay | ReadOnly |
| Seq. Index | `StepIndex` | OneWay | ReadOnly |
| FG Description | `FgName` | OneWay | `TextWrapping`, min 2 lines |
| FG Item Code | `FgItemCode` | OneWay | ReadOnly |
| Remark | `Remark` | TwoWay | `UpdateSourceTrigger=PropertyChanged`; syncs `_remarkFinal` and all `_scaleDataFinal[*].C038` |
| Select Step | `StepCodeMaster` | — | DevExpress `LookUpEdit` with Excel-style column filter popup |
| Barcode / QR Scan | `BarcodeScannedValue` | TwoWay | Status dot (`BarcodeStatus`); `ReadOnly=ReadOnlyScanner`; Enter key replays driver event |

The **Select Step** LookUpEdit popup shows these columns: Machine, Step Code, Step Name, Size, Hydra Order No, FT601 Id.

### 6.3 Right Area

3-row grid (4\* wide):

| Row | Height | Panel |
|-----|--------|-------|
| 0 | 428 px | **TOTAL STEPS** — `dgTotalSteps` DataGrid |
| 1 | Auto | RFID + Barcode row *(Visibility=Collapsed — hidden)* |
| 2 | * | **History** (left half) + **Reference Values / Scale** (right half) |

#### 6.3.1 TOTAL STEPS Grid (`dgTotalSteps`)

`ItemsSource = StepsCollection`. Columns (left to right):

| Column | Width | Binding | Notes |
|--------|-------|---------|-------|
| Actions | 125 | — | **Scale** (`GridScaleCommand`), **Reset** (`GridResetCommand`), **Delete** (`GridDeleteCommand`) buttons |
| STATUS | 65 | `StatusDotColor`, `StatusText` | Ellipse + text |
| MACHINE | 75 | `C004` | Centered |
| MAIN / MAIN CODE | 150 | `C027` (bold) / `C026` (grey sub) | Two-line stacked |
| SEQUENCE STEP | 80 | `C015` | Centered |
| STEP / STEP CODE | 426 | `C003` (bold) / `C002` (grey sub) | Two-line stacked; `TextWrapping` |
| SIZE | 60 | `C008` | Centered |
| TOTAL INJECTION (g) | 100 | `C024` `F2` | Tooltip: "Total weight after ejection. Reading from the scale." |
| TOTAL PART (g/prs) | 60 | `C023` `F2` | Tooltip: "Includes the part of the current step and previous steps." |
| PART (g/prs) | 60 | `C021` `F2` | Tooltip: calculation formula |
| RUNNER / EXCESS MAT. (g/prs) | 100 | `C022` `F2` | Tooltip: calculation formula |
| Actual Partitioning (prs) | 100 | `C028` `F2` | — |
| Actual Pairs Shot (prs) | 80 | `C017` `F2` | — |
| Mold Pairs Shot (prs) | 80 | `C018` `F2` | — |
| Quantity | 70 | `C025` `F2` | — |
| Weight per Unit (g/unit) | 80 | `C036` `F2` | — |
| Unit | 50 | `C037` `F2` | — |
| FG / FG CODE | 763 | `C014` (bold) / `C013` (grey sub) | Two-line stacked; `TextWrapping` |
| MACHINE GROUP | 90 | `C019` | Centered |
| MOLD ID | 90 | `C020` | Centered |
| REMARKS | 90 | `C038` | Centered |
| ARTICLE | 90 | `C005` | Centered |
| HYDRA ORDER | 90 | `C000` | Centered |
| CATEGORY / CATEGORY CODE | 200 | `C034` (bold) / `C033` (grey sub) | Two-line stacked |
| PERCENT OF USAGE (%) | 90 | `C035` | Centered |
| QR CODE | 90 | `C012` | Centered |

#### 6.3.2 History Panel (bottom-left)

- **Header** (dark `#0D1117`): `HistoryToggleText` — click to toggle expand/collapse.
- **Sub-header** row: `HistoryToggleTextDetail` (primary) + `HistoryToggleTextSecond` (secondary, 13 pt).
- **Grid** (`dgHistory`, `Visibility=IsHistoryExpanded`): DATE/TIME, TOTAL WEIGHT (g), TOTAL PART WEIGHT (g/prs), PART WEIGHT (g/prs), RUNNER/EXCESS MAT. WEIGHT (g/prs), OPERATOR.

#### 6.3.3 Reference Values / Scale Panel (bottom-right)

**Header** — `REFERENCE VALUES` (dark bar).

**Scale Value row** (between header and data grid):
- Left: scale status dot + **"SCALE VALUE (g)"** label (30 pt bold).
- Center: large editable `TextBox` (Consolas 50 pt) — bound to `ScaleValue`, `IsReadOnly=ReadOnlyScale`. Manual entry: type → Enter → triggers `ScaleDriver_DataValueChanged`.
- Right: **Stable Value** badge — background from `ScaleStable → StableToForeground` (green = stable, orange = fluctuating).

**Reference Values DataGrid** (`dgRefValues`):
- Background: `ScaleTare → TareToBackground` (indicates tare status).
- Columns: # (circle badge), FIELD (name + unit), STD (coloured by tolerance), ACTUAL (coloured by tolerance), Δ (delta text).
- Below grid: `DeltaInformation` label (20 pt, muted colour).

**Bottom Bar** (60 px, right-aligned):

| Button | Color | Command | Function |
|--------|-------|---------|----------|
| 💾 Save | `#0D1117` | `SaveCommand` | Calc C021/C022/C023/C024, store in memory |
| ❌ Cancel | `#E65100` | `CancelCommand` | Cancel session, reset all state |
| ✔ Confirm | `#2E7D32` | `ConfirmCommand` | Persist to DB, update FT601.C017=true |

**Loading Overlay** — `Grid.RowSpan=3`, semi-transparent black, shows "Loading…" with a Cancel button (`CancelLoadCommand`). `Visibility=IsOverlayVisible`.

---

## 7. FT600 Field Reference

| Field | Type | Description |
|-------|------|-------------|
| id | Guid | Primary key |
| C000 | string | Hydra Order No |
| C002 | string | Step Item Code |
| C003 | string | Step Item Name |
| C004 | string | Machine code |
| C005 | string | Article |
| C008 | string | Size |
| C010 | string | Employee Code (operator) |
| C011 | string | Employee Name |
| C012 | string | Barcode / QR code |
| C013 | string | FG Item Code |
| C014 | string | FG Item Name |
| C015 | int? | Sequence index |
| C017 | decimal? | Actual Pairs per Shot |
| C018 | decimal? | Mold Pairs per Shot |
| C019 | string | Machine Group |
| C020 | string | Mold ID |
| **C021** | double? | **PART WEIGHT (g/prs)** |
| **C022** | double? | **RUNNER / EXCESS MATERIAL WEIGHT (g/prs)** |
| **C023** | double? | **TOTAL PART WEIGHT (g/prs)** |
| **C024** | double? | **TOTAL WEIGHT INJECTION (g)** |
| C025 | double? | Quantity |
| C026 | string | Main Item Code |
| C027 | string | Main Item Name |
| C028 | double? | Actual Partitioning (prs) |
| C029 | Guid? | Label Id (FT606) |
| C032 | Guid? | FT601 Id |
| C033 | string | Category Code |
| C034 | string | Category Name |
| C035 | double? | Percent of Usage (%) |
| C036 | double? | Weight per Unit (g/unit) — Studs, Logo, Cleat_Ring, REX |
| C037 | string | Unit |
| C038 | string | Remarks |
| AllowScale | bool | Whether step can be weighed |
| CreateDate | DateTime | DB save timestamp |
| Mesocomp | string | Company code (VNT1, FKV, FTT1…) |
| Mesoyear | int | Meso production year |

---

## 8. User Guide

### 8.1 Startup and Login

1. Launch SSSW — wait for the "Loading…" overlay to disappear.
2. Check device status dots: 🟢 green = connected, 🔴 red = disconnected.
3. Scan RFID card (or click the employee name in the title bar → `frmRfidInput` dialog opens). Employee name appears next to the RFID status dot.

> **Note:** Only IT / QC staff are authorized. First-time users: type name → Enter → confirm "Yes".

### 8.2 Select a Production Step

**Method 1 – Scan QR/Barcode:** Click the "Barcode / QR Scan" field in the left panel → scan → step auto-selected.  
**Method 2 – Keyboard entry:** Type barcode in the field → press Enter → step auto-selected.  
**Method 3 – Manual:** Click "Select Step" → popup with Excel-style filters → click desired row.

After selection: left panel fills automatically, Total Steps grid shows BOM, History and Reference Values update.

### 8.3 Weighing Workflow

#### Injection — 1st Weighing (C024)
1. Place entire shot (part + runner) on scale. Wait for "Stable Value" badge → green.
2. Click **[Scale]** in the grid row OR press **Save** in the bottom bar.
3. "TOTAL INJECTION (g)" column updates.

#### Injection — 2nd Weighing (C023)
1. Remove the runner, keep only the part on scale. Wait for stability → press **Save**.
2. C023, C022, C021 are calculated. Reference Panel colours by tolerance.

#### Non-Injection (REX) — Single Weighing
1. Place material on scale → wait for stability → press **Save** (once only).
2. System auto-calculates using receptacle or nonwoven formula.

### 8.4 Adjustments

| Adjustment | How | Notes |
|------------|-----|-------|
| Partitioning (prs) | Tick **Adj.** → type number → Enter | When actual pairs differ from Hydra |
| Usage % | Tick **Adj.** → type % → Enter | Only for REX nonwoven/mesh |
| Runner | Select YES / NO in ComboBox | NO → C022 = 0 |
| Remark | Type in Remark field | Auto-syncs C038 on all rows |
| Switch step | Click **[Scale]** on another row | Checks previous step was weighed |
| Reset row | Click **[Reset]** on target row | C021/C022/C023/C024 = 0 |
| Delete row | Click **[Delete]** on target row | Removes from `StepsCollection` |

### 8.5 Manual Scale Entry (scale disconnected or ReadOnly=false)

1. Click inside the large Consolas 50 pt scale value field.
2. Type a numeric value (e.g., `36.5`) — only digits and the decimal point are accepted.
3. Press **Enter** → processed identically to a hardware reading.

> A yellow tooltip appears for invalid characters (e.g., letters); auto-closes after 2 seconds.

### 8.6 Confirm and Save to Database

1. Ensure valid RFID has been scanned (employee shown in title bar).
2. Ensure all `AllowScale=true` steps are weighed (C023 > 0).
3. Click **Confirm** → saves to DB → success message → screen resets for next session.

### 8.7 Hydra Sync

1. Click **⚡ Hydra** button in the title bar.
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
| Warning tooltip (scale box) | Non-numeric character typed or pasted | Use only digits and decimal point |

---

## 10. Converters & Styles

### 10.1 Converters (`ScaleConverters.cs`)

| Converter | Input | Output |
|-----------|-------|--------|
| `ToleranceToBrushConverter` | `ToleranceCategory` | Brush — cell background |
| `ToleranceToBorderBrushConverter` | `ToleranceCategory` | Brush — cell border |
| `ToleranceToForeConverter` | `ToleranceCategory` | Brush — cell text colour |
| `BooleanToVisibilityConverter` | bool | `Visible` / `Collapsed` |
| `DriverStatusToColorConverter` | `DriverStatus` | Brush — device status dot |
| `StableToForegroundConverter` | bool (`IsStable`) | Green = stable, Orange = fluctuating |
| `TareToBackgroundConverter` | bool (`IsTare`) | Scale area background colour |
| `InverseBoolToVisibilityConverter` | bool | Inverse: `false` → `Visible` |

### 10.2 Key Styles (`ShotWeightStyles.xaml`)

| Key | TargetType | Description |
|-----|-----------|-------------|
| `LabelStyle` | TextBlock | Segoe UI 20 pt, colour `#78909C` |
| `FieldStyle` | TextBox | Segoe UI 20 pt, bg `#FAFAFA`, border `#CFD8DC`, ReadOnly |
| `EditableFieldStyle` | TextBox | Inherits `FieldStyle`, `IsReadOnly=false`, white bg |
| `CardStyle` | Border | White bg, border `#CFD8DC`, `CornerRadius=6` |
| `BottomBtn` | Button | Bold 15 pt, rounded, hover/press/disabled states |
| `TitleIconBtn` | Button | Segoe MDL2 Assets icons, 32 × 32 px, hover `#3A4A7A` |
| `GridStyle` | DataGrid | `AutoGenerateColumns=False`, alternating rows, Segoe UI 12 pt |
| `RefRowStyle` | DataGridRow | Conditional row style for Reference Values grid |
| `PrimaryButton` | Button | `#1976D2` — Scale button |
| `WarningButton` | Button | `#F9A825` — Reset button |
| `DangerButton` | Button | `#D32F2F` — Delete button |
| `CenterCell` | TextBlock | Centre-aligned cell element style |
| `cellWrap` | TextBlock | Wrapping cell element style |

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

*SSSW – Shot Weight Station Documentation · framas Internal Use Only · v1.1 – 01/06/2026 · Cong Nguyen · cong.nguyen@framas.com*
