# 🏗️ SSSW Architecture

## Tổng quan

**SSSW** (Scan and Scale Shot Weight) là ứng dụng desktop Windows dùng trên dây chuyền sản xuất framas, phục vụ cân trọng lượng shot weight cho khuôn ép nhựa (injection molding), log kết quả vào SQL Server và đồng bộ ngược về hệ thống MES (Hydra/Winline).

### Tech Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Framework | .NET 8 (net8.0-windows) | 8.0 |
| UI - Active | WPF (MVVM) | - |
| UI - Legacy | WinForms | - |
| ORM | Entity Framework Core | 9.0.11 |
| Database | SQL Server | - |
| UI Controls | DevExpress WinForms + WPF Grid | 24.2.3 |
| Hardware SDK | ScanAndScale.Core / .Driver | 1.1.3 |
| Logging | Serilog (File + Console) | 4.3.0 |
| DI | Microsoft.Extensions.Hosting | 9.0.11 |
| Auto-Update | AutoUpdater.NET | 1.9.2 |
| JSON | Newtonsoft.Json | 13.0.4 |

## Sơ đồ kiến trúc tổng thể

```
┌─────────────────────────────────────────────────────────┐
│                      Program.cs                         │
│  ┌─────────┐  ┌───────────┐  ┌─────────────────────┐   │
│  │ Host/DI │  │ Serilog   │  │ DbContextFactory    │   │
│  └────┬────┘  └───────────┘  └─────────────────────┘   │
│       │                                                 │
│       ▼                                                 │
│  ┌─────────────── Main.xaml (Shell Window) ──────────┐  │
│  │  MainViewModel                                    │  │
│  │  ├── WindowTitle (location + version)             │  │
│  │  ├── OperatorSession (RFID/Employee)              │  │
│  │  ├── DeviceConnectionService (Hardware)           │  │
│  │  ├── HydraCommand / SettingsCommand / Update      │  │
│  │  │                                                │  │
│  │  │  ┌─── Tab: Step Component ───┐                 │  │
│  │  │  │ ShotWeightWindow (UC)     │                 │  │
│  │  │  │ ShotWeightViewModel       │                 │  │
│  │  │  │ • Barcode scan → BOM load │                 │  │
│  │  │  │ • Scale read → Calculate  │                 │  │
│  │  │  │ • Save → Confirm → DB     │                 │  │
│  │  │  └───────────────────────────┘                 │  │
│  │  │                                                │  │
│  │  │  ┌─── Tab: Finished Goods ───┐                 │  │
│  │  │  │ ShotWeightFGWindow (UC)   │                 │  │
│  │  │  │ ShotWeightFGViewModel     │                 │  │
│  │  │  │ • FG sample weighing      │                 │  │
│  │  │  └───────────────────────────┘                 │  │
│  │  │                                                │  │
│  │  └────────────────────────────────────────────────┘  │
│                                                         │
│  ┌──── Shared Services (Singleton) ──────────────────┐  │
│  │ DeviceConnectionService: Barcode/RFID/Scale       │  │
│  │ OperatorSessionService: RFID card → Employee ID   │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  ┌──── Legacy WinForms ──────────────────────────────┐  │
│  │ frmMainView: Lịch sử cân (history viewer)        │  │
│  │ frmUpdateMasterData: Settings/Master data editor  │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘

┌──── External ────────────────────────────────────────────┐
│ SQL Server: DbContextDogeWH (EF Core)                    │
│ ├── FT600  : Shot weight results (main data)             │
│ ├── FT601  : Hydra step items (master)                   │
│ ├── FT602  : Additional data                             │
│ ├── FT605  : Reference data                              │
│ ├── FT606  : Label info (QR code → step mapping)         │
│ ├── FT608  : Machine config (JSON)                       │
│ ├── FT609  : Shot weight sync to Winline                 │
│ ├── FT029  : Operator RFID cards                         │
│ └── FT031  : Departments                                 │
│                                                          │
│ Stored Procedures:                                       │
│ ├── sp_MaterialGetCompanyName                            │
│ ├── sp_MaterialGetMesoyear                               │
│ ├── sp_getBomWinlineOfItemFG                             │
│ ├── sp_GetFullStepItemHydraIsRun                         │
│ ├── sp_GetCategorryOfItem                                │
│ ├── sp_Shotweight_Gets                                   │
│ └── sp_Showweight_UpdateToWinline                        │
│                                                          │
│ Hardware (via ScanAndScale.Core):                         │
│ ├── Barcode Scanner (Zebra CoreScanner SDK)               │
│ ├── RFID Reader (SerialPort / COM)                       │
│ └── Scale (TCP/IP – Shimadzu, Vibra, DIGI, etc.)         │
└──────────────────────────────────────────────────────────┘
```

## Directory Structure

```
SSSW/
├── Program.cs                          # Entry point, DI setup, WPF startup
├── SSSW.csproj                         # Project file (net8.0-windows, WinForms+WPF)
├── DeployConfig.props                  # Deploy path config (fVN/fFT site switch)
├── appsettings.json                    # Connection string (encrypted, gitignored)
│
├── UI/
│   ├── WPF/                            # ═══ Active WPF UI (MVVM) ═══
│   │   ├── Main.xaml / .cs             # Shell window (tab switching)
│   │   ├── ShotWeightWindow.xaml / .cs # Step Component weighing (UserControl)
│   │   ├── ShotWeightFGWindow.xaml/.cs # FG sample weighing (UserControl)
│   │   ├── frmRfidInput.xaml / .cs     # Manual RFID entry dialog
│   │   ├── ViewModels/
│   │   │   ├── BaseViewModel.cs        # INotifyPropertyChanged base
│   │   │   ├── RelayCommand.cs         # ICommand implementations
│   │   │   ├── MainViewModel.cs        # Shell VM (config, hardware, tabs)
│   │   │   ├── ShotWeightViewModel.cs  # Step weighing business logic (~2100 LOC)
│   │   │   ├── ShotWeightFGViewModel.cs# FG weighing business logic
│   │   │   └── frmRfidInputViewModel.cs# RFID input dialog VM
│   │   ├── Models/
│   │   │   └── ScaleModels.cs          # ReferenceRow, ToleranceCategory
│   │   ├── Converters/
│   │   │   └── ScaleConverters.cs      # Tolerance → Color converters
│   │   ├── Services/
│   │   │   ├── DeviceConnectionService.cs  # Hardware singleton
│   │   │   └── OperatorSessionService.cs   # RFID/operator singleton
│   │   └── Styles/
│   │       └── ShotWeightStyles.xaml    # Global WPF styles
│   │
│   ├── frmMainView.cs / .Designer.cs   # Legacy WinForms: history viewer
│   └── frmUpdateMasterData.cs / .Designer.cs # Legacy WinForms: settings
│
├── models/
│   ├── ef/
│   │   ├── DbContextDogeWH.cs          # EF Core DbContext
│   │   ├── GetShotWeightWL_DTO.cs      # SP result DTO
│   │   └── entities/
│   │       ├── BaseEntity.cs           # Base class (Id, Actived, CreatedDate...)
│   │       ├── FT600.cs / .partial.cs  # Shot weight results + UI-only fields
│   │       ├── FT601.cs                # Hydra step items
│   │       ├── FT602.cs                # Additional data
│   │       ├── FT605.cs                # Reference data
│   │       ├── FT606_Label.cs          # QR label info
│   │       ├── FT608_Config.cs         # Machine config
│   │       ├── FT609_ShotWeightUpToWL.cs # Winline sync log
│   │       ├── FT029_Operator_RFID.cs  # RFID operator cards
│   │       └── FT031_Department.cs     # Departments
│   │
│   ├── ConfigModel.cs                  # Hardware + business config (JSON)
│   ├── StepConfigModel.cs              # Step configuration
│   ├── StepSelectModel.cs              # LookUpEdit item model
│   ├── BomWinlineModel.cs              # BOM from Winline SP
│   ├── FGSelectModel.cs                # FG selection model
│   ├── HydraItemDetailModel.cs         # Hydra sync detail
│   ├── HydraItemGroupModel.cs          # Hydra sync group
│   ├── CategoryOfItemModel.cs          # Item category
│   ├── IsScaledModel.cs                # Scale check model
│   ├── ResultModel.cs                  # SP result wrapper
│   └── WeightUpdateModel.cs            # Winline update TVP model
│
├── enums/
│   ├── EnumLocation.cs                 # fVN, fKV, fFT, fIN, fGE
│   ├── EnumSampleLocation.cs           # Production, Sample
│   ├── EnumScaleStatus.cs              # Scale states
│   └── EnumScaleType.cs                # Scale model types
│
├── staticClass/
│   └── GlobalVariable.cs              # Static globals (ConString, Config, Devices)
│
├── docs/                               # 📚 Documentation hub
│   ├── INDEX.md                        # This file
│   ├── architecture/                   # Architecture docs
│   ├── sessions/                       # Work sessions / worklogs
│   ├── brainstorm/                     # Ideas & design discussions
│   ├── plan/                           # Development plans & roadmaps
│   ├── tech/                           # Technical deep-dives
│   ├── manual/                         # End-user manuals
│   └── worklog/                        # Legacy worklogs (pre-standardization)
│
└── Resources/                          # Icons, images
```

## Data Flow: Quy trình cân 1 shot weight

```
 ┌──────────┐    QR Scan      ┌──────────────┐    BOM Query    ┌──────────┐
 │ Barcode  │ ──────────────▶ │ FT606_Label  │ ──────────────▶ │ FT601    │
 │ Scanner  │    C001=QR      │ (QR→Step ID) │   sp_getBom...  │ (Hydra)  │
 └──────────┘                 └──────────────┘                 └────┬─────┘
                                                                    │
                                                        Build _scaleDataFinal
                                                                    │
                                                                    ▼
 ┌──────────┐   Weight Value  ┌──────────────────────────────────────────────┐
 │  Scale   │ ──────────────▶ │            ShotWeightViewModel              │
 │ (TCP/IP) │   OnScaleValue  │                                             │
 └──────────┘   Changed()     │  ExecuteSave():                             │
                              │  ├── 1st weigh: C024 = total weight         │
 ┌──────────┐   Employee ID   │  ├── 2nd weigh: C023 = part weight          │
 │   RFID   │ ──────────────▶ │  │   C021 = part(g/prs), C022 = runner     │
 │  Reader  │   OnRfidValue   │  │   Cascade update later steps             │
 └──────────┘   Changed()     │  └── Auto-advance to next step             │
                              │                                             │
                              │  ExecuteConfirmAsync():                     │
                              │  ├── Validate RFID scanned                  │
                              │  ├── BEGIN TRANSACTION                      │
                              │  ├── INSERT FT600s (weight results)         │
                              │  ├── UPDATE FT601.C017 = true (done flag)   │
                              │  ├── COMMIT                                 │
                              │  └── Reset for next loop                    │
                              └──────────────────────────────────────────────┘
```

## DI Container (Program.cs)

| Service | Lifetime | Mô tả |
|---------|----------|-------|
| `IDbContextFactory<DbContextDogeWH>` | Scoped | EF Core factory (thread-safe) |
| `DeviceConnectionService` | **Singleton** | Barcode/RFID/Scale hardware |
| `OperatorSessionService` | **Singleton** | RFID/operator identity chung Step/FG |
| `MainViewModel` | Transient | Shell window ViewModel |
| `Main` | Transient | Shell window (real top-level Window) |
| `ShotWeightViewModel` | Transient | Step weighing ViewModel |
| `ShotWeightWindow` | Transient | Step weighing UserControl |
| `ShotWeightFGViewModel` | Transient | FG weighing ViewModel |
| `ShotWeightFGWindow` | Transient | FG weighing UserControl |
| `frmRfidInputViewModel` | Transient | Manual RFID entry ViewModel |
| `frmRfidInput` | Transient | Manual RFID entry dialog |
| `frmMainView` | Transient | History viewer (WinForms) |
| `frmUpdateMasterData` | Transient | Settings editor (WinForms) |

## Multi-site Configuration

| Site | mesocomp | EnumLocation | Deploy Share |
|------|----------|-------------|-------------|
| Vietnam | VNT1 | fVN | `\\192.168.1.241\...` |
| KV | FKV | fKV | - |
| **FT (Active)** | FTT1 | fFT | `\\10.40.10.9\...` |
| India | 05FI | fIN | - |
| Germany | fGE | fGE | - |

Cấu hình site hiện tại: **fFT** (xem `DeployConfig.props`)

## Startup Sequence

```
1. Program.Main()
   ├── ApplicationConfiguration.Initialize()
   ├── Set InvariantCulture
   ├── Load appsettings.json → Decrypt connection string
   ├── Setup Serilog (rolling daily, 30-day retention, 10MB limit)
   ├── Build Host + DI container
   ├── Create scope → Resolve Main window
   └── wpfApp.Run(wpfWin) ←── Starts WPF message pump

2. Main.xaml.cs → Window.Loaded event
   └── MainViewModel.StartupAsync()
       ├── Load mesocomp/mesoyear from SP → WindowTitle
       ├── Load FT608_Config for this machine → GlobalVariable.ConfigSystem
       ├── DeviceConnectionService.EnsureInitialized() → Hardware connect
       ├── OperatorSession.Attach() → Subscribe RFID events
       └── SelectStep() → Open Step Component tab (default)

3. ShotWeightWindow.OnLoaded
   └── ShotWeightViewModel.InitializeAsync()
       ├── Load mesocomp/mesoyear (for this VM's context)
       ├── Set defaults from Config
       └── LoadDataAsync() → Query FT601s
```
