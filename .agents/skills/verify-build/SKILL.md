---
name: verify-build
description: >-
  Build, compile, and verify the SSSW solution in Debug and Release configurations.
  Use this skill to verify code changes, check for build errors or warnings, and validate
  auto-update packaging targets before committing.
---

# 🛠️ SSSW Build & Verification Runbook

This skill outlines how to build and verify the SSSW .NET 8 application.

## Prerequisites
- Windows OS (WPF and Windows Forms workloads enabled).
- .NET 8 SDK installed.

## Build Commands

### 1. Fast Debug Build
Run:
```powershell
dotnet build SSSW/SSSW.sln
```
Expected: `Build succeeded. 0 Error(s)`.

### 2. Release Build (Auto-Update Packaging)
> [!NOTE]
> A Release build executes custom MSBuild targets in `SSSW/SSSW.csproj`:
> 1. `GenerateUpdateXml` (creates `update.xml` with timestamped version).
> 2. `CreateUpdateZip` (zips build artifacts into `Update.zip`).
> 3. `CopyUpdateToShare` (copies zip & xml to the network share specified in `DeployConfig.props`).

Run:
```powershell
dotnet build SSSW/SSSW.sln -c Release
```
If the network share is unavailable (e.g. offline dev machine), `CopyUpdateToShare` may report an error if `$(DeployDir)` does not exist. This is expected when not connected to the factory VPN/LAN.

## Verification Checklist
- [ ] No compilation errors.
- [ ] No unresolved ambiguous references between WPF (`System.Windows.Media`) and WinForms (`System.Drawing`).
- [ ] No new compiler warnings introduced in modified files.
