# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

SSSW ("Scan and Scale Shot Weight") is a Windows desktop app used on production-floor workstations at framas to weigh injection-molding shots against BOM standards, log results to SQL Server, and push status back to the Hydra/Winline MES. It's a hybrid WinForms + WPF .NET 8 app: WPF (`ShotWeightWindow` / MVVM) is the active production screen; several WinForms screens (`frmShotWeightScale`, `frmShotWeightScaleV2`, `frmMainView`, `frmUpdateMasterData`) are legacy/auxiliary and still registered in DI but not launched by default.

The actual source project is `SSSW/` (namespace `SSSW`). `Template/` holds unrelated scaffold examples (WPF MVVM starter, clean-architecture starter) — not part of the shipping app. `WinFormsApp1/` is a throwaway sample project — ignore both unless specifically asked to work in them.

## Commands

Build/run from the `SSSW/` directory (or pass `SSSW/SSSW.sln` / `SSSW/SSSW.csproj` explicitly):

```
dotnet build SSSW/SSSW.sln            # Debug build
dotnet build SSSW/SSSW.sln -c Release # Release build — also triggers the update-package pipeline (see below)
dotnet run --project SSSW/SSSW.csproj
```

There is no test project in this repo — there are no `dotnet test` targets to run.

Target framework: `net8.0-windows` (WinForms + WPF both enabled). Requires the Windows desktop workload; this only builds/runs on Windows.

### Release build side effects

A Release build (`-c Release`) runs MSBuild targets defined in [SSSW.csproj](SSSW/SSSW.csproj) that fire `AfterTargets="Build"`:
1. `GenerateUpdateXml` — writes `update.xml` with the current `$(Version)` (auto-derived from date/time: `VersionPrefix.MMdd.HHmm`).
2. `CreateUpdateZip` — zips the build output into `Update.zip` (consumed by AutoUpdater.NET at runtime).
3. `CopyUpdateToShare` — copies both files to a hardcoded network share (`DeployDir`, currently the fFT site path) and **errors out (`<Error>`) if that share isn't reachable**.

If you build Release without access to that network share, expect the build to fail on `CopyUpdateToShare` — this is expected on a dev machine off the corporate network, not a code bug. Debug builds skip all three targets.

### Local secrets

`appsettings.json` is gitignored (see root `.gitignore` — it also strips `bin/`, `obj/`, `**/net8.0-windows/`, and `.dll/.exe/.pdb`). The SQL Server connection string in `ConnectionStrings:SSSW` is stored encrypted (`EncodeMD5.DecryptString(...)` in `Program.cs`, decrypted with a hardcoded key). Don't commit a plaintext `appsettings.json`.

## Architecture

### Entry point and DI

[Program.cs](SSSW/Program.cs) is the single entry point for both UI stacks. It builds a generic `Host` with `Microsoft.Extensions.Hosting`, registers Serilog (rolling daily file log under `Logs/`, 30-day retention), and configures DI:
- `IDbContextFactory<DbContextDogeWH>` (EF Core, SQL Server) — used everywhere instead of injecting `DbContext` directly, because WinForms/WPF UI code needs short-lived, thread-safe contexts per operation.
- All Forms (`frmShotWeightScale`, `frmMainView`, `frmUpdateMasterData`, `frmShotWeightScaleV2`) and the WPF `ShotWeightWindow` + `ShotWeightViewModel` are registered as transient.

Only one UI actually launches: the WPF path (`wpfApp.Run(wpfWin)` on `ShotWeightWindow`) is active; the WinForms `Application.Run(mainForm)` alternative is commented out in `Main()`. Switching between them means editing `Program.cs` directly — there's no config flag.

Config (`ConfigModel`, in [models/ConfigModel.cs](SSSW/models/ConfigModel.cs)) covers scanner/RFID/scale hardware settings and business tunables (tolerance deltas, non-woven usage %). It's loaded per-machine from `FT608_Config` (`WHERE MachineName = Environment.MachineName`), JSON-deserialized, and cached on the static `GlobalVariable.ConfigSystem` ([staticClass/GlobalVariable.cs](SSSW/staticClass/GlobalVariable.cs)). If no row exists for the machine, defaults are inserted.

### The WPF screen (active production UI)

`ShotWeightWindow.xaml` / `.xaml.cs` / `ShotWeightViewModel.cs` under [UI/WPF/](SSSW/UI/WPF/) is the real app. Full architecture, business rules, calculation formulas, DB flow, and UI layout are documented in detail in [SSSW/ShotWeightWindow_Documentation_EN.md](SSSW/ShotWeightWindow_Documentation_EN.md) (also available in Vietnamese: `ShotWeightWindow_TaiLieuKyThuat_EN.html` / `.html`) — **read that file before making non-trivial changes to the weighing screen**; it covers things a code read-through won't surface quickly:
- The MVVM split: View has zero business logic; code-behind only does what can't be bound (Win32 drag, DevExpress bridging, hardware driver → Dispatcher.Invoke bridging, input validation); all business logic/state/commands live in `ShotWeightViewModel`.
- Three hardware drivers (`BarcodeDriver`, `RfidDriver`, `ScaleDriver`, from the external `ScanAndScale.Driver`/`ScanAndScale.Core` packages, referenced both as NuGet and via `../Dll/net8.0-windows/ScanAndScale.Core.dll`) fire events on background threads — **all UI/VM updates from those handlers must go through `Dispatcher.Invoke`/`InvokeAsync`**, done in the code-behind.
- The `Action<T>` delegate bridge pattern the ViewModel uses for operations that can't be data-bound (focusing a DevExpress grid row, clearing a LookUpEdit, etc.) — wired up in `OnLoaded`.
- The weight calculation formulas (different for 1st vs 2nd injection weighing, receptacle REX items, non-woven/mesh REX items, and multi-size shared molds) and the cascade recalculation of later steps after a Save.
- The tolerance banding (`ToleranceCategory`: Idle/Ok/Warn/Err at ±1%/±3%, in [UI/WPF/Models/ScaleModels.cs](SSSW/UI/WPF/Models/ScaleModels.cs)) driving cell coloring via converters in [UI/WPF/Converters/ScaleConverters.cs](SSSW/UI/WPF/Converters/ScaleConverters.cs).
- The Confirm flow's DB transaction (insert `FT600` rows, `ExecuteUpdateAsync` on `FT601.C017`, rollback on error).

Note the doc is dated 2026-06-01 (v1.1); the ViewModel's step-filtering / `AllowScale` logic has been actively refactored since (see recent commit history) — treat the doc as the architectural map, but verify current filtering/AllowScale behavior against the live code in `ShotWeightViewModel.cs` rather than assuming the doc's step-3.3/`AllowScale` wording is exact.

### Data layer

`DbContextDogeWH` ([models/ef/DbContextDogeWH.cs](SSSW/models/ef/DbContextDogeWH.cs)) is the single EF Core `DbContext`, targeting SQL Server. Entities live in `models/ef/entities/` and are named after their physical table codes (`FT600`, `FT601`, `FT602`, `FT605`, `FT606_Label`, `FT608_Config`, `FT029_Operator_RFID`, `FT031_Department`, `FT609_ShotWeightUpToWL`) — most business fields are opaque `C0xx` columns; see the FT600 field reference table in `ShotWeightWindow_Documentation_EN.md` §7 for what each column means before touching them. Most entities have a global `HasQueryFilter(p => p.Actived == true)` soft-delete filter applied in `OnModelCreating`.

`FT600` has a `[NotMapped]` partial extension ([models/ef/entities/FT600.partial.cs](SSSW/models/ef/entities/FT600.partial.cs)) adding UI-only fields (`AllowScale`, `StatusText`, `StatusDotColor`, `StatusBarColor`) — these live only in memory/UI, never persisted.

Several stored procedures are called directly rather than modeled as DbSets (BOM lookup, Hydra sync, company/year lookup, category lookup) — see doc §11 for the full list and parameters.

### Models directory conventions

- `models/*.cs` — plain DTOs/POCOs used across the app (`ConfigModel`, `StepConfigModel`, `StepSelectModel`, `BomWinlineModel`, `HydraItemDetailModel`, etc.). `SSSW.models` namespace.
- `models/ef/entities/` — EF Core entities mapped to SQL Server tables. `SSSW.modelss` namespace (note the double-s — inconsistent with `models/ef/`, this is existing convention, not a typo to silently fix).
- `enums/` — small standalone enums (`EnumLocation`, `EnumScaleType`, `EnumScaleStatus`, `EnumSampleLocation`).
- `UI/WPF/Models/` — WPF-only presentation models (`ReferenceRow`, `ToleranceCategory`) that implement `INotifyPropertyChanged` themselves — distinct from the `models/` DTOs.

### Multi-site config

`GlobalVariable` maps DB-reported company code (`mesocomp`) to `EnumLocation` (`fVN`/`fKV`/`fFT`/`fIN`/`fGE`) which drives the window title and the `DeployDir` used for auto-update packaging — this repo currently targets the fFT site's share path. `DeployDir` itself lives in [SSSW/DeployConfig.props](SSSW/DeployConfig.props) (imported by `SSSW.csproj` via `<Import Project="DeployConfig.props" />`, not inlined in the csproj) — switch sites by editing the `<DeployDir>` block there (comment/uncomment fFT vs fVN), no `.csproj` edit needed. `appsettings.json`'s connection string still has its own commented `fVN`/`fFT` blocks and needs updating separately when switching target sites — the two are not linked.

### Worklog

[docs/worklog/](SSSW/docs/worklog/) holds a dated, per-task log of work done on this repo (what was asked, what changed, assumptions made, open questions, build result) — separate from the architecture docs above, which describe current-state design rather than change history. **Check [docs/worklog/INDEX.md](SSSW/docs/worklog/INDEX.md) at the start of a session** (at least the most recent entry, more if continuing related work) before re-deriving context from git log/code alone. Append a new entry (don't edit old ones) after any non-trivial task — see the conventions at the bottom of `INDEX.md`.
